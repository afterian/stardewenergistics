using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewEnergistics.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Blocks;

/// <summary>
/// Right-click (action button) on network blocks:
/// Storage Rack takes/gives disks, Power Rack takes Battery Packs, Coal Generator takes fuel,
/// Core shows network status, Terminal opens its menu. A Chest Link is attached by using it on a chest
/// and removed with the Network Wrench.
/// </summary>
internal sealed class BlockInteraction
{
    private readonly IModHelper helper;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;
    private readonly TerminalController terminal;
    private readonly TextureRegistry textures;

    public BlockInteraction(IModHelper helper, NetworkManager networks, DiskStore disks, PowerService power, TerminalController terminal, TextureRegistry textures)
    {
        this.textures = textures;
        this.helper = helper;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
        this.terminal = terminal;
    }

    public void Register() => helper.Events.Input.ButtonPressed += OnButtonPressed;

    private string T(string key, object? tokens = null) => helper.Translation.Get(key, tokens);

    private static void Hud(string text) => Game1.addHUDMessage(new HUDMessage(text) { noIcon = true });

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree || helper.Input.IsSuppressed(e.Button))
            return;
        // Controllers often "use" a held item with the tool button (X) instead of the action button (A).
        bool useButton = e.Button.IsUseToolButton() && Game1.player.CurrentTool == null
            && !IsHeld(Game1.player.ActiveObject, Ids.NetworkWrench) && IsOurAttachable(Game1.player.ActiveObject);
        if (!e.Button.IsActionButton() && !useButton)
            return;

        var location = Game1.currentLocation;
        var tile = e.Cursor.GrabTile;

        if (Game1.player.ActiveObject?.QualifiedItemId == "(O)" + Ids.WirelessTerminal)
        {
            helper.Input.Suppress(e.Button);
            OpenWireless(location);
            return;
        }
        if (HandleMainBin(e.Button, location, tile))
            return;

        if (FindTarget(location, e.Cursor.GrabTile) is not { } found)
            return;
        tile = found.Tile;
        var obj = found.Obj;
        // With the tool button, only act on our own blocks (and our attachments' targets); leave vanilla use alone.
        if (!e.Button.IsActionButton() && BlockIds.KindOf(obj) == null && !Game1.player.ActiveObject!.QualifiedItemId.StartsWith("(O)" + Ids.Prefix))
            return;

        var held = Game1.player.ActiveObject;

        // Machine and Shipping Links (checked first: a Mini-Shipping Bin is a chest).
        if (HandleLinks(e.Button, location, tile, obj, held))
            return;

        if (HandlePowerFeed(e.Button, location, tile, obj, held))
            return;

        // Chest Link on / off.
        if (obj is Chest chest)
        {
            if (held?.QualifiedItemId == "(O)" + Ids.ChestLink)
            {
                helper.Input.Suppress(e.Button);
                if (chest.modData.ContainsKey(Ids.Data.ChestLink))
                {
                    Hud(T("chestLink.already"));
                    return;
                }
                chest.modData[Ids.Data.ChestLink] = "1";
                Game1.player.reduceActiveItemByOne();
                networks.Invalidate(location);
                Game1.playSound("hammer");
                Hud(T("chestLink.added"));
            }
            else if (held?.QualifiedItemId == "(O)" + Ids.NetworkWrench && chest.modData.ContainsKey(Ids.Data.ChestLink))
            {
                helper.Input.Suppress(e.Button);
                chest.modData.Remove(Ids.Data.ChestLink);
                Game1.createItemDebris(ItemRegistry.Create("(O)" + Ids.ChestLink), tile * 64 + new Vector2(32, 32), -1, location);
                networks.Invalidate(location);
                Game1.playSound("hammer");
                Hud(T("chestLink.removed"));
            }
            return;
        }

        switch (BlockIds.KindOf(obj))
        {
            case BlockKind.StorageRack:
                helper.Input.Suppress(e.Button);
                StorageRack(obj, held);
                break;
            case BlockKind.PowerRack:
                helper.Input.Suppress(e.Button);
                PowerRack(location, obj, held);
                break;
            case BlockKind.CoalGenerator:
                helper.Input.Suppress(e.Button);
                Generator(location, obj, held);
                break;
            case BlockKind.Core:
                helper.Input.Suppress(e.Button);
                ShowStatus(location, tile);
                break;
            case BlockKind.Terminal:
                helper.Input.Suppress(e.Button);
                power.ShowTerminals(networks.For(location));
                terminal.Open(location, tile);
                break;
            case BlockKind.Port:
                helper.Input.Suppress(e.Button);
                ShowPort(location, tile, obj);
                break;
            case BlockKind.CraftingRack:
                helper.Input.Suppress(e.Button);
                ShowJobs(location, tile);
                break;
        }
    }

    /// <summary>
    /// The object the player means: the tile under the cursor (or the base of a big craftable whose top half
    /// was clicked), else the tile the player faces. If neither has anything, look at the 8 tiles around the
    /// player (facing side first), like the game does for controllers, but only accept our own blocks or a
    /// target for the mod item in hand, so vanilla interactions aren't taken over.
    /// </summary>
    private static (Vector2 Tile, SObject Obj)? FindTarget(GameLocation location, Vector2 cursorTile)
    {
        foreach (var t in new[] { cursorTile, Game1.player.GetGrabTile() })
        {
            if (At(location, t) is { } hit)
                return hit;
        }

        // Only controllers target nearby tiles; a mouse click means the clicked tile.
        if (Game1.lastCursorMotionWasMouse && Game1.mouseCursorTransparency > 0)
            return null;

        var held = Game1.player.ActiveObject;
        var me = Game1.player.Tile;
        var facing = Game1.player.GetGrabTile() - me;
        var around = new List<Vector2>();
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0)
                    around.Add(new Vector2(dx, dy));
        foreach (var d in around.OrderBy(d => Vector2.DistanceSquared(d, facing)))
        {
            if (At(location, me + d) is { } hit && (BlockIds.KindOf(hit.Obj) != null || (IsOurAttachable(held) && held!.QualifiedItemId.StartsWith("(O)" + Ids.Prefix))))
                return hit;
        }
        return null;
    }

    private static (Vector2 Tile, SObject Obj)? At(GameLocation location, Vector2 t)
    {
        if (location.objects.TryGetValue(t, out var obj))
            return (t, obj);
        var below = new Vector2(t.X, t.Y + 1);
        if (location.objects.TryGetValue(below, out obj) && obj.bigCraftable.Value)
            return (below, obj);
        return null;
    }

    /// <summary>Held items this mod puts into or onto things.</summary>
    private static bool IsOurAttachable(SObject? held) => held != null && (
        held.QualifiedItemId.StartsWith("(O)" + Ids.Prefix)
        || held.QualifiedItemId is "(O)" + VanillaIds.BatteryPack or "(O)" + VanillaIds.Coal or "(O)" + VanillaIds.Hardwood);

    private const string MiniShippingBin = "(BC)248";

    private static bool IsHeld(SObject? held, string id) => held?.QualifiedItemId == "(O)" + id;

    /// <summary>Attach a Link by using it on a machine or Mini-Shipping Bin; the wrench opens its settings.</summary>
    private bool HandleLinks(SButton button, GameLocation location, Vector2 tile, SObject obj, SObject? held)
    {
        bool hasMachineLink = obj.modData.ContainsKey(Ids.Data.MachineLink);
        bool hasShippingLink = obj.modData.ContainsKey(Ids.Data.ShippingLink);

        if (IsHeld(held, Ids.MachineLink) || IsHeld(held, Ids.ShippingLink))
        {
            bool shipping = IsHeld(held, Ids.ShippingLink);
            bool fits = shipping ? obj.QualifiedItemId == MiniShippingBin : MachineDriver.IsMachine(obj);
            if (!fits)
            {
                // Let other handlers (and the game) deal with clicks on non-targets, but explain on our own blocks.
                if (obj is not Chest && BlockIds.KindOf(obj) == null && !shipping)
                    return false;
                helper.Input.Suppress(button);
                Hud(T(shipping ? "link.notBin" : "link.notMachine"));
                return true;
            }
            helper.Input.Suppress(button);
            if (hasMachineLink || hasShippingLink || obj.modData.ContainsKey(Ids.Data.ChestLink))
            {
                Hud(T("link.already"));
                return true;
            }
            obj.modData[shipping ? Ids.Data.ShippingLink : Ids.Data.MachineLink] = "1";
            obj.modData[Ids.Data.LinkState] = MachineLinkService.NotSetUp.ToString();
            Game1.player.reduceActiveItemByOne();
            networks.Invalidate(location);
            Game1.playSound("hammer");
            Hud(T("link.added"));
            return true;
        }

        if (IsHeld(held, Ids.NetworkWrench) && (hasMachineLink || hasShippingLink))
        {
            helper.Input.Suppress(button);
            OpenLinkSettings(location, tile, obj, hasShippingLink);
            return true;
        }
        return false;
    }

    /// <summary>A Shipping Link on the farm's main Shipping Bin: use the Link on the bin, open its settings with the wrench.</summary>
    private bool HandleMainBin(SButton button, GameLocation location, Vector2 tile)
    {
        var held = Game1.player.ActiveObject;
        if (!IsHeld(held, Ids.ShippingLink) && !IsHeld(held, Ids.NetworkWrench))
            return false;
        if (location.getBuildingAt(tile) is not StardewValley.Buildings.ShippingBin bin)
            return false;

        bool linked = bin.modData.ContainsKey(Ids.Data.ShippingLink);
        if (IsHeld(held, Ids.ShippingLink))
        {
            helper.Input.Suppress(button);
            if (linked)
            {
                Hud(T("link.already"));
                return true;
            }
            bin.modData[Ids.Data.ShippingLink] = "1";
            bin.modData[Ids.Data.LinkState] = MachineLinkService.NotSetUp.ToString();
            Game1.player.reduceActiveItemByOne();
            networks.Invalidate(location);
            Game1.playSound("hammer");
            Hud(T("link.added.bin"));
            return true;
        }

        if (!linked)
            return false;
        helper.Input.Suppress(button);
        OpenSettingsMenu(location, tile, bin.modData, shipping: true, T("link.title.shipping"));
        return true;
    }

    private void OpenLinkSettings(GameLocation location, Vector2 tile, SObject obj, bool shipping)
    {
        string title;
        if (shipping)
            title = T("link.title.shipping");
        else
        {
            int count = MachineGroupFinder.Group(new TilePos((int)tile.X, (int)tile.Y), p => MachineDriver.MachineIdAt(location, p.X, p.Y)).Count;
            title = T("link.title.machine", new { machine = obj.DisplayName, count });
        }

        OpenSettingsMenu(location, tile, obj.modData, shipping, title);
    }

    /// <summary>The Link settings screen for the Link whose data is <paramref name="data"/> (on an object or the main bin).</summary>
    private void OpenSettingsMenu(GameLocation location, Vector2 tile, StardewValley.Mods.ModDataDictionary data, bool shipping, string title)
    {
        Game1.activeClickableMenu = new LinkSettingsMenu(
            helper.Translation, textures.Get, data, shipping, title, MachineLinkService.SettingsOf(data),
            onSave: (target, settings) =>
            {
                MachineLinkService.SetSettings(target, settings);
                if (!settings.Configured)
                    target[Ids.Data.LinkState] = MachineLinkService.NotSetUp.ToString();
                else if (target.TryGetValue(Ids.Data.LinkState, out string? st) && st == MachineLinkService.NotSetUp.ToString())
                    target[Ids.Data.LinkState] = MachineLinkService.Idle.ToString();
            },
            onRemove: target =>
            {
                string item = target.ContainsKey(Ids.Data.ShippingLink) ? Ids.ShippingLink : Ids.MachineLink;
                target.Remove(Ids.Data.MachineLink);
                target.Remove(Ids.Data.ShippingLink);
                target.Remove(Ids.Data.LinkSettings);
                target.Remove(Ids.Data.LinkState);
                Game1.createItemDebris(ItemRegistry.Create("(O)" + item), tile * 64 + new Vector2(32, 32), -1, location);
                networks.Invalidate(location);
                Game1.playSound("hammer");
                Hud(T("link.removed"));
            });
    }

    private void StorageRack(SObject rack, SObject? held)
    {
        var slots = RackSlots.Get(rack, Ids.Data.RackDisks);
        if (DiskStore.IsDisk(held))
        {
            if (slots.Count >= RackSlots.SlotsPerRack)
            {
                Hud(T("rack.full"));
                return;
            }
            var contents = disks.ForItem(held!);
            slots.Add($"{contents.Id.Value}:{contents.Tier}");
            RackSlots.Set(rack, Ids.Data.RackDisks, slots);
            RackSlots.ShowFrame(rack, slots.Count);
            Game1.player.reduceActiveItemByOne();
            Game1.playSound("Ship");
            Hud(DescribeDisk(contents, held!.DisplayName));
            networks.Invalidate(Game1.currentLocation);
            return;
        }

        if (slots.Count == 0)
        {
            Hud(T("rack.empty"));
            return;
        }

        // Take out the last disk, with its contents.
        string last = slots[^1];
        string[] parts = last.Split(':');
        var tier = parts.Length > 1 && System.Enum.TryParse<StardewEnergistics.Core.Storage.DiskTier>(parts[1], out var t) ? t : StardewEnergistics.Core.Storage.DiskTier.Copper;
        var disk = disks.CreateItem(disks.Get(parts[0], tier));
        if (!Game1.player.couldInventoryAcceptThisItem(disk))
        {
            Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
            return;
        }
        slots.RemoveAt(slots.Count - 1);
        RackSlots.Set(rack, Ids.Data.RackDisks, slots);
        RackSlots.ShowFrame(rack, slots.Count);
        Game1.player.addItemToInventoryBool(disk);
        Game1.playSound("pickUpItem");
        networks.Invalidate(Game1.currentLocation);
    }

    private string DescribeDisk(StardewEnergistics.Core.Storage.DiskContents d, string name) => T("disk.contents", new
    {
        name,
        kinds = d.KindsUsed,
        maxKinds = d.Spec.UnlimitedKinds ? "∞" : d.Spec.MaxKinds.ToString(),
        items = d.TotalItems.ToString("N0"),
        maxItems = d.Spec.MaxItems.ToString("N0"),
    });

    private void PowerRack(GameLocation location, SObject rack, SObject? held)
    {
        var charge = RackSlots.GetInts(rack, Ids.Data.RackCharge);
        if (held?.QualifiedItemId == "(O)" + VanillaIds.BatteryPack)
        {
            if (charge.Count >= RackSlots.SlotsPerRack)
            {
                Hud(T("rack.full"));
                return;
            }
            charge.Add(power.Table.BatteryPower);
            RackSlots.SetInts(rack, Ids.Data.RackCharge, charge);
            RackSlots.ShowFrame(rack, charge.Count);
            Game1.player.reduceActiveItemByOne();
            Game1.playSound("Ship");
            ResetPoweredFlag(location, rack.TileLocation);
        }
        Hud(T("power.rack", new { count = charge.Count, charge = charge.Sum() }));
        WarnIfNotConnected(location, rack.TileLocation);
    }

    private void Generator(GameLocation location, SObject gen, SObject? held)
    {
        int fuel = RackSlots.GetInt(gen, Ids.Data.Fuel);
        int per = held?.QualifiedItemId switch
        {
            "(O)" + VanillaIds.Coal => power.Table.CoalPower,
            "(O)" + VanillaIds.Hardwood => power.Table.HardwoodPower,
            _ => 0,
        };
        if (per > 0)
        {
            fuel += per * held!.Stack;
            Game1.player.removeItemFromInventory(held);
            gen.modData[Ids.Data.Fuel] = fuel.ToString();
            RackSlots.ShowFrame(gen, 1);
            Game1.playSound("fireball");
            ResetPoweredFlag(location, gen.TileLocation);
        }
        Hud(T("power.generator", new { fuel }));
        WarnIfNotConnected(location, gen.TileLocation);
    }

    /// <summary>Power added to a block that isn't joined to a Computer Core does nothing: say why.</summary>
    private void WarnIfNotConnected(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        if (n == null || !loc.IsActive(n))
        {
            Game1.showRedMessage(T("power.notConnected"));
            return;
        }
        if (!power.IsPowered(loc, n) && networks.Home(loc, n) is { } home)
            Hud(power.Describe(home.Loc, home.Net));
    }

    /// <summary>After adding power mid-day, let the network re-check whether it has enough.</summary>
    private void ResetPoweredFlag(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        if (loc.NetworkAt(tile) is { } n)
            power.Recheck(loc, n);
    }

    private static readonly HashSet<string> FeedTargets = new() { "(BC)9", "(BC)231" }; // Lightning Rod, Solar Panel

    private bool HandlePowerFeed(SButton button, GameLocation location, Vector2 tile, SObject obj, SObject? held)
    {
        if (IsHeld(held, Ids.PowerFeed))
        {
            helper.Input.Suppress(button);
            if (!FeedTargets.Contains(obj.QualifiedItemId))
            {
                Hud(T("feed.notTarget"));
                return true;
            }
            if (obj.modData.ContainsKey(Ids.Data.PowerFeed) || obj.modData.ContainsKey(Ids.Data.MachineLink))
            {
                Hud(T("link.already"));
                return true;
            }
            obj.modData[Ids.Data.PowerFeed] = "1";
            Game1.player.reduceActiveItemByOne();
            networks.Invalidate(location);
            Game1.playSound("hammer");
            Hud(T("feed.added"));
            return true;
        }
        if (IsHeld(held, Ids.NetworkWrench) && obj.modData.ContainsKey(Ids.Data.PowerFeed))
        {
            helper.Input.Suppress(button);
            obj.modData.Remove(Ids.Data.PowerFeed);
            Game1.createItemDebris(ItemRegistry.Create("(O)" + Ids.PowerFeed), tile * 64 + new Vector2(32, 32), -1, location);
            networks.Invalidate(location);
            Game1.playSound("hammer");
            Hud(T("feed.removed"));
            return true;
        }
        return false;
    }

    private void ShowPort(GameLocation location, Vector2 tile, SObject port)
    {
        if (!location.IsOutdoors)
        {
            RackSlots.ShowFrame(port, 0);
            Hud(T("port.outdoorsOnly"));
            return;
        }
        var building = NetworkManager.BuildingBeside(location, tile);
        if (building?.GetIndoors() is not { } indoors)
        {
            RackSlots.ShowFrame(port, 0);
            Hud(T("port.noBuilding"));
            return;
        }
        RackSlots.ShowFrame(port, 1);
        Hud(T("port.linked", new { building = indoors.DisplayName }));
    }

    /// <summary>Opens the Terminal of the network here, or of the farmhouse when on the farm.</summary>
    private void OpenWireless(GameLocation location)
    {
        if (FirstBlockTile(location) is { } tile)
        {
            terminal.Open(location, tile);
            return;
        }
        bool onFarm = location.IsFarm || location.ParentBuilding != null;
        var home = Game1.getLocationFromName("FarmHouse");
        if (onFarm && home != null && FirstBlockTile(home) is { } homeTile)
        {
            terminal.Open(home, homeTile);
            return;
        }
        Game1.showRedMessage(T("wireless.none"));
    }

    /// <summary>A tile of some block in an active network here (the Core's network first).</summary>
    private Vector2? FirstBlockTile(GameLocation location)
    {
        var loc = networks.For(location);
        var n = loc.Graph.Networks.Where(loc.IsActive).OrderByDescending(x => x.Working).FirstOrDefault();
        var b = n?.Core ?? n?.Blocks.FirstOrDefault();
        return b == null ? null : new Vector2(b.Pos.X, b.Pos.Y);
    }

    private void ShowJobs(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        if (n?.Core == null)
        {
            Hud(T("auto.noRack"));
            return;
        }
        var jobs = StardewEnergistics.Crafting.CraftingService.JobsOf(loc.ObjectOf(n.Core)).Jobs.Where(j => !j.Finished).ToList();
        string list = jobs.Count == 0
            ? T("auto.noJobs")
            : string.Join(", ", jobs.Select(j => $"{j.Target.Count} {StardewEnergistics.Crafting.CraftingService.NameOf(j.Target.Item.QualifiedId)} ({(int)(j.Progress * 100)}%)"));
        Hud(T("rack.crafting", new { jobs = list }));
    }

    private void ShowStatus(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        if (n == null)
            return;

        var issue = loc.Graph.Issues.FirstOrDefault(i => n.Blocks.Any(b => b.Id == i.BlockId));
        if (issue != null)
        {
            Game1.showRedMessage(T($"network.problem.{issue.Kind}"));
            return;
        }

        var storage = NetworkStorage.For(loc, n, disks);
        Game1.drawObjectDialogue(T("network.status", new
        {
            blocks = n.Blocks.Count,
            disks = storage.Disks.Disks.Count,
            kinds = storage.KindsStored,
            items = storage.Disks.TotalItems.ToString("N0"),
            power = power.Describe(loc, n),
        }));
    }
}
