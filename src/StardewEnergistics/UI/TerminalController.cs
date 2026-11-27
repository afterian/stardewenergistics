using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace StardewEnergistics.UI;

/// <summary>
/// Opens the Network Terminal. For now it reuses vanilla menus:
/// <list type="bullet">
/// <item>Storage: a chest screen showing the network's disk items (most plentiful first, up to 70 stacks).
/// Take and put as with any chest; on close, everything goes back into network storage.</item>
/// <item>Craft: the vanilla crafting page with network storage as its ingredient source (paused without power).</item>
/// </list>
/// A custom Terminal menu with search, scrolling and quality rows replaces this later in M2.
/// </summary>
internal sealed class TerminalController
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;

    private OpenSession? session;

    public TerminalController(IModHelper helper, IMonitor monitor, NetworkManager networks, DiskStore disks, PowerService power)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
    }

    private sealed record OpenSession(IClickableMenu Menu, NetworkStorage Storage, Inventory Temp, Chest? TempChest);

    public void Register()
    {
        helper.Events.Display.MenuChanged += OnMenuChanged;
    }

    private string T(string key) => helper.Translation.Get(key);

    public void Open(GameLocation location, Vector2 tile)
    {
        if (!Context.IsMainPlayer)
        {
            Game1.showRedMessage("Terminals for farmhands arrive with multiplayer support later in M2.");
            return;
        }

        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        if (n == null || !n.Working)
        {
            Game1.showRedMessage(T("terminal.noNetwork"));
            return;
        }

        var responses = new[]
        {
            new Response("storage", T("terminal.tab.storage")),
            new Response("craft", T("terminal.tab.craft")),
            new Response("cancel", Game1.content.LoadString("Strings\\Locations:MineCart_Destination_Cancel")),
        };
        location.createQuestionDialogue(T("terminal.title"), responses, (_, answer) =>
        {
            // The question dialogue closes first; open the next menu on the next tick.
            if (answer == "storage")
                DelayedAction.functionAfterDelay(() => OpenStorage(loc, n), 1);
            else if (answer == "craft")
                DelayedAction.functionAfterDelay(() => OpenCraft(loc, n), 1);
        });
    }

    private void OpenStorage(LocationNetworks loc, StardewEnergistics.Core.Graph.Network n)
    {
        var storage = NetworkStorage.For(loc, n, disks);
        var chest = new Chest(true, "BigChest");
        var temp = storage.ExtractToTemp(maxStacks: chest.GetActualCapacity());
        foreach (var item in temp)
            chest.Items.Add(item);

        chest.ShowMenu();
        if (Game1.activeClickableMenu is ItemGrabMenu menu)
            session = new OpenSession(menu, storage, chest.Items, chest);
        else
            ReturnItems(storage, chest.Items);
    }

    private void OpenCraft(LocationNetworks loc, StardewEnergistics.Core.Graph.Network n)
    {
        if (!power.IsPowered(loc, n))
        {
            Game1.showRedMessage(helper.Translation.Get("network.unpowered"));
            return;
        }

        var storage = NetworkStorage.For(loc, n, disks);
        var temp = storage.ExtractToTemp();
        var materials = new List<IInventory> { temp };
        materials.AddRange(storage.Chests.Select(c => (IInventory)c.Items));

        var pos = Utility.getTopLeftPositionForCenteringOnScreen(800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2);
        var menu = new CraftingPage((int)pos.X, (int)pos.Y, 800 + IClickableMenu.borderWidth * 2, 600 + IClickableMenu.borderWidth * 2, false, true, materials);
        Game1.activeClickableMenu = menu;
        session = new OpenSession(menu, storage, temp, null);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (session == null || e.OldMenu != session.Menu)
            return;

        // The chest screen re-opens itself after each take/put; keep the session going.
        if (session.TempChest != null && e.NewMenu is ItemGrabMenu next && next.context == session.TempChest)
        {
            session = session with { Menu = next };
            return;
        }

        var s = session;
        session = null;
        ReturnItems(s.Storage, s.Temp);
    }

    private void ReturnItems(NetworkStorage storage, IEnumerable<Item?> temp)
    {
        var leftovers = storage.ReturnFromTemp(temp.ToList());
        foreach (var item in leftovers)
        {
            if (!Game1.player.addItemToInventoryBool(item))
                Game1.createItemDebris(item, Game1.player.getStandingPosition(), Game1.player.FacingDirection, Game1.player.currentLocation);
        }
        if (leftovers.Count > 0)
            Game1.showRedMessage(T("terminal.full"));
        monitor.Log($"Terminal closed; {leftovers.Count} stacks didn't fit back.", LogLevel.Trace);
    }
}
