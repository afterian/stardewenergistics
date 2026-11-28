using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Content;
using StardewEnergistics.Crafting;
using StardewEnergistics.Multiplayer;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace StardewEnergistics.UI;

/// <summary>
/// Opens the Network Terminal: the <see cref="TerminalMenu"/> storage screen, and from it the vanilla
/// crafting page with network storage as its ingredient source (paused without power).
/// The host and split-screen players use storage directly; online farmhands go through the host.
/// </summary>
internal sealed class TerminalController
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;
    private readonly TextureRegistry textures;
    private readonly TerminalNet net;
    private readonly CraftingService crafting;

    private readonly PerScreen<CraftSession?> craftSession = new();

    public TerminalController(IModHelper helper, IMonitor monitor, NetworkManager networks, DiskStore disks, PowerService power, TextureRegistry textures, TerminalNet net, CraftingService crafting)
    {
        this.crafting = crafting;
        this.helper = helper;
        this.monitor = monitor;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
        this.textures = textures;
        this.net = net;
    }

    private sealed record CraftSession(IClickableMenu Menu, NetworkStorage Storage, Inventory Temp);

    public void Register() => helper.Events.Display.MenuChanged += OnMenuChanged;

    private string T(string key) => helper.Translation.Get(key);

    /// <summary>Host and split-screen players share the host's memory, so they read storage directly.</summary>
    private static bool HasLocalState => Context.IsMainPlayer || Context.IsSplitScreen;

    public void Open(GameLocation location, Vector2 tile)
    {
        ITerminalBackend backend;
        if (HasLocalState)
        {
            var loc = networks.For(location);
            var n = loc.NetworkAt(tile);
            if (n == null || !loc.IsActive(n))
            {
                Game1.showRedMessage(T("terminal.noNetwork"));
                return;
            }
            backend = new LocalTerminalBackend(() => Resolve(location, tile), disks, power);
        }
        else
        {
            backend = net.RemoteBackend(location, tile);
        }

        Game1.activeClickableMenu = new TerminalMenu(backend, helper.Translation, textures.Get, () => OpenCraft(location, tile), () => OpenAutocraft(location, tile));
    }

    private (LocationNetworks, StardewEnergistics.Core.Graph.Network)? Resolve(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        return n != null ? networks.Home(loc, n) : null;
    }

    private void OpenAutocraft(GameLocation location, Vector2 tile)
    {
        if (Resolve(location, tile) is not { } home)
        {
            Game1.showRedMessage(T("terminal.noNetwork"));
            return;
        }

        bool advanced = CraftingService.IsAdvanced(home.Item2);
        var book = crafting.Book(advanced);
        var craftable = book.Outputs.ToList();
        var backend = new AutocraftBackend(
            Craftable: () => craftable,
            Plan: (key, n) => Resolve(location, tile) is { } r ? crafting.Plan(r.Item1, r.Item2, key, n) : null,
            Start: (key, n) => Resolve(location, tile) is { } r ? crafting.Start(r.Item1, r.Item2, key, n) : T("terminal.noNetwork"),
            Jobs: () => Resolve(location, tile) is { } r && r.Item2.Core != null ? CraftingService.JobsOf(r.Item1.ObjectOf(r.Item2.Core)).Jobs : System.Array.Empty<StardewEnergistics.Core.Crafting.CraftJob>(),
            Cancel: id => Resolve(location, tile) is { } r && crafting.Cancel(r.Item1, r.Item2, id),
            Advanced: advanced,
            StockRules: () => Resolve(location, tile) is { } r ? CraftingService.StockRulesOf(r.Item1, r.Item2) : new List<StardewEnergistics.Core.Crafting.StockRule>(),
            SetStockRule: (key, n) => Resolve(location, tile) is { } r ? crafting.SetStockRule(r.Item1, r.Item2, key, n) : T("terminal.noNetwork"));
        // Closing autocraft goes back to the Terminal's storage screen.
        Game1.activeClickableMenu = new AutocraftMenu(backend, helper.Translation) { exitFunction = () => Open(location, tile) };
    }

    private void OpenCraft(GameLocation location, Vector2 tile)
    {
        var resolved = Resolve(location, tile);
        if (resolved == null)
        {
            Game1.showRedMessage(T("terminal.noNetwork"));
            return;
        }
        var (loc, n) = resolved.Value;
        if (!power.IsPowered(loc, n))
        {
            Game1.showRedMessage(T("network.unpowered"));
            return;
        }

        var storage = NetworkStorage.For(loc, n, disks);
        var temp = storage.ExtractToTemp();
        var materials = new List<IInventory> { temp };
        materials.AddRange(storage.Chests.Select(c => (IInventory)c.Items));

        int w = 800 + IClickableMenu.borderWidth * 2, h = 600 + IClickableMenu.borderWidth * 2;
        var pos = Utility.getTopLeftPositionForCenteringOnScreen(w, h);
        var menu = new CraftingPage((int)pos.X, (int)pos.Y, w, h, false, true, materials);
        craftSession.Value = new CraftSession(menu, storage, temp);
        // Closing the crafting page (X, Escape or B) goes back to the Terminal's storage screen.
        menu.exitFunction = () =>
        {
            FinishCraft();
            Open(location, tile);
        };
        Game1.activeClickableMenu = menu;
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        // Fallback for a page closed without its exit function (end of day, a forced close).
        if (craftSession.Value is { } s && e.OldMenu == s.Menu)
            FinishCraft();
    }

    /// <summary>Puts the crafting page's unused ingredients back into network storage.</summary>
    private void FinishCraft()
    {
        var s = craftSession.Value;
        if (s == null)
            return;
        craftSession.Value = null;

        var leftovers = s.Storage.ReturnFromTemp(s.Temp.ToList());
        foreach (var item in leftovers)
            TerminalItems.Give(item);
        if (leftovers.Count > 0)
            Game1.showRedMessage(T("terminal.full"));
        monitor.Log($"Crafting page closed; {leftovers.Count} stacks didn't fit back.", LogLevel.Trace);
    }
}
