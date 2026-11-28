using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Machines;
using StardewEnergistics.Storage;
using StardewEnergistics.World;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using static StardewEnergistics.Content.VanillaIds;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>One end-to-end scenario. <see cref="ThrowawayOnly"/> ones change state outside the scratch area (e.g. a day's power pass on every network), so they only run in a session launched for the self-test.
/// <see cref="OnlyWhenNamed"/> ones (the showcase) are left out of a plain run.</summary>
internal sealed record Scenario(string Id, string Title, Func<SelfTestContext, IEnumerable<object?>> Run, bool ThrowawayOnly = false, bool OnlyWhenNamed = false);

/// <summary>The self-test's scenarios. Each starts from an empty scratch area and a cleared backpack.</summary>
internal static class Scenarios
{
    private static string O(string id) => "(O)" + id;
    private static string BC(string id) => "(BC)" + id;

    private const string Stone = "(O)390";
    private const string Wood = "(O)388";
    private const string CopperOre = "(O)378";
    private const string CopperBarQ = "(O)334";
    private const string FiberQ = "(O)771";
    private const string CoalQ = "(O)382";
    private const string Furnace = "(BC)13";
    private const string ChestQ = "(BC)130";

    /// <summary>Plain stackable objects for filling a disk's kinds (20 = a Copper Disk's limit, plus spares).</summary>
    private static readonly string[] Kinds =
    {
        "(O)390", "(O)388", "(O)330", "(O)771", "(O)382", "(O)378", "(O)380", "(O)384", "(O)334", "(O)335",
        "(O)336", "(O)92", "(O)709", "(O)80", "(O)338", "(O)24", "(O)192", "(O)190", "(O)188", "(O)250",
        "(O)18", "(O)20", "(O)16", "(O)22",
    };

    public static readonly IReadOnlyList<Scenario> All = new List<Scenario>
    {
        new("coverage", "Every item has data, a recipe and a play test; every scenario is on the coverage list", Coverage),
        new("placement", "Place every block and break it again", Placement),
        new("wire", "Lay wire with mouse and controller; pick it up", Wire),
        new("wire-placement", "Where wire goes with mouse and controller: floor, chests, machines, flooring, soil; walls, trees, water refused", WirePlacement.Run),
        new("network", "Core, racks and Terminal join one powered network", Network),
        new("disks", "Insert and remove disks in a Storage Rack", Disks),
        new("terminal", "Store and withdraw through the Terminal screen", Terminal),
        new("capacity", "Disk kind and item limits; space frees on withdraw", Capacity),
        new("targeting", "Controller targeting and big-craftable top halves", Targeting),
        new("break-rack", "Breaking racks and generators drops their contents", BreakRack),
        new("chest-link", "Chest Link on, storage overflow, wrench off", ChestLink),
        new("power", "Unpowered networks, batteries, fuel and recheck", Power),
        new("power-day", "A morning power pass drains batteries", PowerDay, ThrowawayOnly: true),
        new("machine-link", "Machine Link feeds a Furnace from storage", MachineLink),
        new("autocraft", "Crafting Rack autocrafts Copper Wire", Autocraft),
        new("tools", "Plain tools go on disks through the Terminal", Tools),
        new("press", "Processor Press makes all three processors; missing inputs are refused", FeatureScenarios.Press),
        new("heavy-cable", "Heavy Cable lays, changes colour, hides and picks up", FeatureScenarios.HeavyCable),
        new("disk-upgrade", "Crafting a disk from the tier below keeps its ID and contents", FeatureScenarios.DiskUpgrade, ThrowawayOnly: true),
        new("shipping-link", "Shipping Link ships from storage into the farm's bin in the evening", FeatureScenarios.ShippingLink, ThrowawayOnly: true),
        new("power-feed", "Power Feed moves a Lightning Rod's battery into the Power Rack", FeatureScenarios.PowerFeed),
        new("wireless", "Wireless Terminal opens the network from anywhere in the building", FeatureScenarios.Wireless),
        new("keep-stock", "Advanced Crafting Rack keeps an item in stock overnight", FeatureScenarios.KeepInStock, ThrowawayOnly: true),
        new("save", "Save data and the game's own save format keep everything", Save),
        new("quest", "Maru's journal letter, tier letters teach recipes, journal goals tick", Quest.Run, ThrowawayOnly: true),
        new("playthrough", "The whole mod from day one to the journal's last page, as a player", Playthrough.Run, ThrowawayOnly: true),
        new("showcase", "The playthrough slowly, with screenshots, leaving the base standing", Playthrough.Run, ThrowawayOnly: true, OnlyWhenNamed: true),
    };

    // ---------------------------------------------------------------

    private static IEnumerable<object?> Coverage(SelfTestContext c)
    {
        var listed = Core.Testing.PlayTestCoverage.ScenarioIds;
        var ids = All.Select(s => s.Id).ToList();
        c.Check("every scenario is on the coverage list", ids.All(listed.Contains), "not listed: " + string.Join(", ", ids.Where(i => !listed.Contains(i))));
        c.Check("every scenario on the coverage list exists", listed.All(ids.Contains), "missing: " + string.Join(", ", listed.Where(i => !ids.Contains(i))));
        foreach (string qid in Content.ContentInjector.AllQualifiedItemIds)
        {
            string key = qid[(qid.IndexOf(')') + 1)..].Replace(Ids.Prefix, "");
            c.Check($"{key}: item data loaded", ItemRegistry.GetData(qid) != null);
            c.Check($"{key}: has a play test", !Core.Testing.PlayTestCoverage.Missing(new[] { "item:" + key }).Any());
        }
        var recipes = StardewValley.CraftingRecipe.craftingRecipes;
        foreach (string r in Content.ContentInjector.AllRecipes.Append(Ids.QuestJournal))
        {
            c.Check($"{r.Replace(Ids.Prefix, "")}: recipe in the game's data", recipes.ContainsKey(r));
            if (recipes.ContainsKey(r))
            {
                var recipe = new StardewValley.CraftingRecipe(r, false);
                var bad = recipe.recipeList.Keys.Where(k => ItemRegistry.GetData(k) == null).ToList();
                c.Check($"{r.Replace(Ids.Prefix, "")}: every ingredient exists", bad.Count == 0, string.Join(", ", bad));
            }
        }
        yield break;
    }

    private static IEnumerable<object?> Placement(SelfTestContext c)
    {
        var blocks = new[] { Ids.ComputerCore, Ids.StorageRack, Ids.PowerRack, Ids.CoalGenerator, Ids.NetworkTerminal, Ids.CraftingRack };
        for (int i = 0; i < blocks.Length; i++)
        {
            c.Place(BC(blocks[i]), c.T(i, 0));
            c.CheckEqual($"{blocks[i].Replace(Ids.Prefix, "")}: hand empty after placing", 0, c.Carried(BC(blocks[i])));
        }
        yield return null;

        for (int i = 0; i < blocks.Length; i++)
        {
            bool broke = c.Break(c.T(i, 0), new Pickaxe());
            c.Check($"{blocks[i].Replace(Ids.Prefix, "")}: pickaxe breaks it", broke);
        }
        yield return Wait.Ticks(2);
        var dropped = c.Debris();
        foreach (string b in blocks)
            c.CheckEqual($"{b.Replace(Ids.Prefix, "")}: dropped exactly once", 1, dropped.Count(d => d.QualifiedItemId == BC(b)));
    }

    private static IEnumerable<object?> Wire(SelfTestContext c)
    {
        var layer = c.S.Wires.For(c.Loc);

        // Mouse: wire goes where the cursor is.
        c.StandAt(c.T(0, 2), Game1.up);
        c.Hold(O(Ids.CopperWire), 10);
        bool taken = c.Input.Press(SButton.MouseLeft, c.T(1, 1));
        c.Check("mouse: press is taken", taken);
        c.Check("mouse: wire on the cursor tile", layer.Has(SelfTestContext.P(c.T(1, 1))));
        c.CheckEqual("mouse: one wire used", 9, c.Carried(O(Ids.CopperWire)));

        // Visible cursor further away (but within 5 tiles) still means the cursor tile.
        c.StandAt(c.T(0, 4), Game1.up);
        c.Input.Press(SButton.MouseLeft, c.T(4, 4), c.Player.GetGrabTile());
        c.Check("mouse 4 tiles away: wire on the cursor tile", layer.Has(SelfTestContext.P(c.T(4, 4))));

        // Cursor beyond reach: the tile in front of the player.
        c.StandAt(c.T(0, 4), Game1.up);
        c.Input.Press(SButton.MouseLeft, c.T(7, 4), c.Player.GetGrabTile());
        c.Check("mouse 7 tiles away: not on the cursor tile", !layer.Has(SelfTestContext.P(c.T(7, 4))));
        c.Check("mouse 7 tiles away: on the tile in front", layer.Has(SelfTestContext.P(c.T(0, 3))));

        // Controller, cursor hidden: the tile the player faces, wherever the (stale) cursor is.
        c.StandAt(c.T(6, 2), Game1.up);
        c.Input.Press(SButton.ControllerX, c.T(9, 5), c.Player.GetGrabTile(), mouse: false);
        c.Check("controller: wire on the facing tile", layer.Has(SelfTestContext.P(c.T(6, 1))));
        c.Check("controller: not on the hidden cursor's tile", !layer.Has(SelfTestContext.P(c.T(9, 5))));

        // Pointing at the top half of a big craftable lays wire under it.
        var core = c.Place(BC(Ids.ComputerCore), c.T(8, 1));
        c.Hold(O(Ids.CopperWire), 5);
        c.StandAt(c.T(7, 2), Game1.right);
        c.Input.Press(SButton.MouseLeft, c.T(8, 0));
        c.Check("top half: wire under the block", layer.Has(SelfTestContext.P(c.T(8, 1))));
        c.Check("top half: not on the tile above", !layer.Has(SelfTestContext.P(c.T(8, 0))));
        c.Check("top half: Core sits on wire in a network", core != null && c.NetworkAt(c.T(8, 1))?.Core != null);
        yield return null;

        // Pickaxe picks wire up from a bare tile.
        c.StandAt(c.T(1, 2), Game1.up);
        c.Hold(new Pickaxe());
        c.Input.Press(SButton.MouseLeft, c.T(1, 1));
        c.Check("pickaxe: wire removed", !layer.Has(SelfTestContext.P(c.T(1, 1))));
        yield return Wait.Ticks(1);
        c.CheckEqual("pickaxe: one wire dropped", 1, c.Debris().Where(d => d.QualifiedItemId == O(Ids.CopperWire)).Sum(d => d.Stack));
    }

    private static IEnumerable<object?> Network(SelfTestContext c)
    {
        var (core, rack, power, terminal) = c.BuildBasicNetwork();
        var n = c.NetworkAt(c.T(0, 0));
        c.Require("Core is on a network", n?.Core != null);
        foreach (var (name, tile) in new[] { ("Storage Rack", c.T(1, 0)), ("Power Rack", c.T(2, 0)), ("Terminal", c.T(3, 0)) })
            c.Check($"{name} on the Core's network", c.NetworkAt(tile)?.Id == n!.Id);
        c.Check("network is active", c.Nets.IsActive(n!));
        c.CheckEqual("two Battery Packs in the Power Rack", 2, RackSlots.GetInts(power, Ids.Data.RackCharge).Count);
        c.CheckEqual("Battery Packs left the backpack", 0, c.Carried(O(BatteryPack)));
        c.Check("network is powered", c.S.Power.IsPowered(c.Nets, n!));
        c.Check("no network problems", !c.Nets.Graph.Issues.Any(), string.Join(", ", c.Nets.Graph.Issues.Select(i => i.Kind)));

        // Breaking the wire under the Terminal leaves it beside the rack's wire, so it still connects (own tile or next to it).
        yield return null;
        c.Hold(new Pickaxe());
        var layer = c.S.Wires.For(c.Loc);
        layer.Remove(SelfTestContext.P(c.T(5, 0)));
        c.S.Networks.Invalidate(c.Loc);
        c.Check("removing a spare wire end keeps the network", c.NetworkAt(c.T(3, 0))?.Id == c.NetworkAt(c.T(0, 0))?.Id);

        // A second Core on the same wire stops the network.
        c.Place(BC(Ids.ComputerCore), c.T(4, 0));
        c.S.Networks.Invalidate(c.Loc);
        var two = c.NetworkAt(c.T(0, 0));
        c.Check("two Cores: network stops", two != null && !two.Working);
        c.Check("two Cores: a problem is reported", c.Nets.Graph.Issues.Any());
    }

    private static IEnumerable<object?> Disks(SelfTestContext c)
    {
        var (_, rack, _, _) = c.BuildBasicNetwork();
        var rackTile = c.T(1, 0);
        var disk1 = c.NewDisk();
        string id1 = SelfTestContext.DiskIdOf(disk1);

        c.Check("insert with mouse: press taken", c.InsertDisk(rackTile, disk1));
        var slots = RackSlots.Get(rack, Ids.Data.RackDisks);
        c.CheckEqual("rack holds one disk", 1, slots.Count);
        c.Check("rack holds that disk's ID", slots.FirstOrDefault()?.StartsWith(id1) == true, string.Join(",", slots));
        c.CheckEqual("disk left the backpack", 0, c.Carried(O(Ids.CopperDisk)));
        c.CheckEqual("network sees one disk", 1, c.StorageAt(c.T(3, 0))?.Disks.Disks.Count ?? -1);

        var disk2 = c.NewDisk(Ids.IronDisk);
        c.Check("insert with controller: press taken", c.InsertDisk(rackTile, disk2, mouse: false));
        c.CheckEqual("rack holds two disks", 2, RackSlots.Get(rack, Ids.Data.RackDisks).Count);
        yield return null;

        // Put something on the disks, then take the last one out with an empty hand.
        c.StorageAt(c.T(3, 0))!.Put(ItemRegistry.Create(Stone, 30));
        long before = SelfTestContext.Stored(c.StorageAt(c.T(3, 0)), Stone);
        c.StandBeside(rackTile);
        c.HoldNothing();
        c.Action(rackTile);
        c.S.Networks.Invalidate(c.Loc);
        c.CheckEqual("empty hand takes one disk out", 1, RackSlots.Get(rack, Ids.Data.RackDisks).Count);
        var back = c.CarriedItems(O(Ids.IronDisk)).ToList();
        c.CheckEqual("the Iron Disk is back in the backpack", 1, back.Count);
        c.Check("it is the same disk", back.Count == 1 && SelfTestContext.DiskIdOf(back[0]) == SelfTestContext.DiskIdOf(disk2));
        c.CheckEqual("items on the remaining disk are still stored", before, SelfTestContext.Stored(c.StorageAt(c.T(3, 0)), Stone));

        // Fill the rack; a fifth disk is refused and stays in hand.
        foreach (var d in new[] { back[0], c.NewDisk(), c.NewDisk() })
            c.InsertDisk(rackTile, d);
        c.CheckEqual("rack is full at 4", RackSlots.SlotsPerRack, RackSlots.Get(rack, Ids.Data.RackDisks).Count);
        var fifth = c.NewDisk();
        c.InsertDisk(rackTile, fifth);
        c.CheckEqual("fifth disk refused: rack still 4", RackSlots.SlotsPerRack, RackSlots.Get(rack, Ids.Data.RackDisks).Count);
        c.CheckEqual("fifth disk refused: still in hand", 1, c.Carried(O(Ids.CopperDisk)));
    }

    private static IEnumerable<object?> Terminal(SelfTestContext c)
    {
        c.BuildBasicNetwork();
        c.InsertDisk(c.T(1, 0), c.NewDisk());
        var term = c.T(3, 0);

        c.Player.Items[3] = ItemRegistry.Create(Stone, 50);
        c.Player.Items[4] = ItemRegistry.Create(Wood, 10);
        var menu = c.OpenTerminal(term);
        c.Require("Terminal opens", menu != null, Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        yield return null;

        c.ClickInventorySlot(menu!, 3);
        c.CheckEqual("store stack: backpack has no Stone", 0, c.Carried(Stone));
        c.CheckEqual("store stack: 50 Stone stored (once)", 50L, SelfTestContext.Stored(c.StorageAt(term), Stone));

        c.ClickInventorySlot(menu!, 4, right: true);
        c.CheckEqual("store one: 9 Wood left", 9, c.Carried(Wood));
        c.CheckEqual("store one: 1 Wood stored", 1L, SelfTestContext.Stored(c.StorageAt(term), Wood));
        yield return null;

        c.Check("Stone is listed", c.ClickStoredItem(menu!, Stone));
        c.CheckEqual("withdraw stack: 50 Stone back", 50, c.Carried(Stone));
        c.CheckEqual("withdraw stack: none left stored", 0L, SelfTestContext.Stored(c.StorageAt(term), Stone));
        var disk = c.StorageAt(term)!.Disks.Disks[0];
        c.CheckEqual("withdraw stack: disk total drops", 1L, disk.TotalItems);
        c.CheckEqual("withdraw stack: Stone's kind is freed", 1, disk.KindsUsed);

        c.Check("Wood is listed", c.ClickStoredItem(menu!, Wood, right: true));
        c.CheckEqual("withdraw one: 10 Wood carried", 10, c.Carried(Wood));
        c.CheckEqual("withdraw one: disk empty", 0L, disk.TotalItems);
        c.CheckEqual("withdraw one: no kinds used", 0, disk.KindsUsed);

        // With no disks at all, a stored item comes straight back with a reason.
        c.CloseMenu();
        c.StandBeside(c.T(1, 0));
        c.HoldNothing();
        c.Action(c.T(1, 0)); // take the disk out
        c.S.Networks.Invalidate(c.Loc);
        menu = c.OpenTerminal(term);
        c.Require("Terminal reopens", menu != null);
        int slot = c.SlotOf(Stone);
        Game1.hudMessages.Clear();
        c.ClickInventorySlot(menu!, slot);
        c.CheckEqual("no disks: Stone comes back (exactly once)", 50, c.Carried(Stone));
        c.Check("no disks: says why", c.Showed(c.T("terminal.noDisks")), c.Messages());
        c.CloseMenu();
    }

    private static IEnumerable<object?> Capacity(SelfTestContext c)
    {
        c.BuildBasicNetwork();
        c.InsertDisk(c.T(1, 0), c.NewDisk());
        var term = c.T(3, 0);
        var storage = c.StorageAt(term)!;
        var disk = storage.Disks.Disks[0];
        int maxKinds = disk.Spec.MaxKinds;

        for (int i = 0; i < maxKinds; i++)
            storage.Put(ItemRegistry.Create(Kinds[i], 1));
        c.CheckEqual("disk holds its kind limit", maxKinds, disk.KindsUsed);

        string extra = Kinds[maxKinds];
        c.Player.Items[5] = ItemRegistry.Create(extra, 7);
        var menu = c.OpenTerminal(term);
        c.Require("Terminal opens", menu != null);
        Game1.hudMessages.Clear();
        c.ClickInventorySlot(menu!, 5);
        c.CheckEqual("kinds full: the item comes back once", 7, c.Carried(extra));
        c.CheckEqual("kinds full: nothing stored", 0L, SelfTestContext.Stored(c.StorageAt(term), extra));
        c.Check("kinds full: says why", c.Showed(c.T("terminal.kindsFull")), c.Messages());
        yield return null;

        // Taking every one of a kind out frees the kind at once.
        c.Check("first kind is listed", c.ClickStoredItem(menu!, Kinds[0]));
        c.CheckEqual("withdraw frees a kind", maxKinds - 1, disk.KindsUsed);
        int slot = c.SlotOf(extra);
        c.ClickInventorySlot(menu!, slot);
        c.CheckEqual("freed kind: the new item is stored", 7L, SelfTestContext.Stored(c.StorageAt(term), extra));
        c.CheckEqual("freed kind: none left in the backpack", 0, c.Carried(extra));
        yield return null;

        // Item limit: fill up with Stone (already a kind) to the disk's total.
        long room = disk.Spec.MaxItems - disk.TotalItems;
        storage = c.StorageAt(term)!;
        for (long left = room; left > 0; left -= 999)
            storage.Put(ItemRegistry.Create(Kinds[1], (int)Math.Min(999, left)));
        c.CheckEqual("disk is at its item limit", disk.Spec.MaxItems, disk.TotalItems);
        c.Player.Items[6] = ItemRegistry.Create(Kinds[1], 5);
        int carriedBefore = c.Carried(Kinds[1]);
        Game1.hudMessages.Clear();
        c.ClickInventorySlot(menu!, 6);
        c.CheckEqual("items full: the stack comes back once", carriedBefore, c.Carried(Kinds[1]));
        c.Check("items full: says why", c.Showed(c.T("terminal.full")), c.Messages());

        // Withdrawing frees item space at once.
        c.CloseMenu();
        menu = c.OpenTerminal(term);
        c.ClickStoredItem(menu!, Kinds[1]);
        c.Check("withdraw frees item space", disk.TotalItems < disk.Spec.MaxItems, $"{disk.TotalItems}/{disk.Spec.MaxItems}");
        slot = c.SlotOf(Kinds[1]);
        int carried = c.Carried(Kinds[1]);
        long storedBefore = SelfTestContext.Stored(c.StorageAt(term), Kinds[1]);
        c.Player.Items[slot] = ItemRegistry.Create(Kinds[1], 5);
        c.ClickInventorySlot(menu!, slot);
        c.CheckEqual("freed space: 5 more stored", storedBefore + 5, SelfTestContext.Stored(c.StorageAt(term), Kinds[1]));
        c.CloseMenu();
    }

    private static IEnumerable<object?> Targeting(SelfTestContext c)
    {
        // Core on the left, a lone Storage Rack at x = 4, wire between.
        c.LayWire(c.Row(0, 0, 4));
        c.Place(BC(Ids.ComputerCore), c.T(0, 0));
        var rack = c.Place(BC(Ids.StorageRack), c.T(4, 0))!;

        // Controller, standing diagonally below-right of the rack, facing away, cursor hidden somewhere else.
        c.StandAt(c.T(5, 1), Game1.down);
        c.Hold(c.NewDisk());
        bool taken = c.Input.Press(SButton.ControllerA, c.T(9, 5), c.Player.GetGrabTile(), mouse: false);
        c.Check("controller: diagonal rack takes the press", taken);
        c.CheckEqual("controller: disk went into the diagonal rack", 1, RackSlots.Get(rack, Ids.Data.RackDisks).Count);
        yield return null;

        // Mouse on an empty far tile: nothing happens, even with a rack beside the player.
        c.StandAt(c.T(5, 1), Game1.down);
        c.Hold(c.NewDisk());
        taken = c.Input.Press(SButton.MouseRight, c.T(8, 4), c.Player.GetGrabTile());
        c.Check("mouse on empty tile: press not taken", !taken);
        c.CheckEqual("mouse on empty tile: rack unchanged", 1, RackSlots.Get(rack, Ids.Data.RackDisks).Count);

        // Mouse on the rack's top half means the rack.
        c.StandAt(c.T(4, 1), Game1.up);
        taken = c.Input.Press(SButton.MouseRight, c.T(4, -1));
        c.Check("top half click: taken", taken);
        c.CheckEqual("top half click: disk inserted", 2, RackSlots.Get(rack, Ids.Data.RackDisks).Count);

        // Controller facing the rack directly.
        c.StandAt(c.T(4, 1), Game1.up);
        c.Hold(c.NewDisk());
        c.Input.Press(SButton.ControllerA, c.T(0, 5), c.Player.GetGrabTile(), mouse: false);
        c.CheckEqual("controller facing: disk inserted", 3, RackSlots.Get(rack, Ids.Data.RackDisks).Count);

        // Battery Pack with the controller's tool button (X) also goes in.
        var power = c.Place(BC(Ids.PowerRack), c.T(2, 0))!;
        c.StandAt(c.T(2, 1), Game1.up);
        c.Hold(O(BatteryPack), 3);
        c.Input.Press(SButton.ControllerX, c.T(9, 5), c.Player.GetGrabTile(), mouse: false);
        c.CheckEqual("controller X: Battery Pack goes into the Power Rack", 1, RackSlots.GetInts(power, Ids.Data.RackCharge).Count);
        c.CheckEqual("controller X: one Battery Pack used", 2, c.Carried(O(BatteryPack)));
    }

    private static IEnumerable<object?> BreakRack(SelfTestContext c)
    {
        var (_, rack, power, _) = c.BuildBasicNetwork();
        var rackTile = c.T(1, 0);
        var d1 = c.NewDisk();
        var d2 = c.NewDisk(Ids.GoldDisk);
        c.InsertDisk(rackTile, d1);
        c.InsertDisk(rackTile, d2);
        var storage = c.StorageAt(c.T(3, 0))!;
        storage.Put(ItemRegistry.Create(Stone, 30));
        storage.Put(ItemRegistry.Create(Wood, 20));
        var ids = new[] { SelfTestContext.DiskIdOf(d1), SelfTestContext.DiskIdOf(d2) };
        long stone = SelfTestContext.Stored(storage, Stone), wood = SelfTestContext.Stored(storage, Wood);

        c.Require("pickaxe breaks the Storage Rack", c.Break(rackTile, new Pickaxe()));
        yield return Wait.Ticks(2);
        var dropped = c.Debris();
        var disks = dropped.Where(DiskStore.IsDisk).ToList();
        c.CheckEqual("both disks dropped", 2, disks.Count);
        c.Check("dropped disks keep their IDs", ids.All(id => disks.Any(d => SelfTestContext.DiskIdOf(d) == id)), string.Join(",", disks.Select(SelfTestContext.DiskIdOf)));
        var rackItems = dropped.Where(d => d.QualifiedItemId == BC(Ids.StorageRack)).ToList();
        c.CheckEqual("the rack dropped once", 1, rackItems.Count);
        c.Check("dropped rack holds no disks", rackItems.All(r => !r.modData.ContainsKey(Ids.Data.RackDisks)));

        // Pick it all up, put the rack back, re-insert: everything is still there.
        c.PickUpDebris();
        c.Place(BC(Ids.StorageRack), rackTile);
        foreach (var d in c.Player.Items.Where(DiskStore.IsDisk).ToList())
            c.InsertDisk(rackTile, d!);
        storage = c.StorageAt(c.T(3, 0))!;
        c.CheckEqual("re-inserted: Stone intact", stone, SelfTestContext.Stored(storage, Stone));
        c.CheckEqual("re-inserted: Wood intact", wood, SelfTestContext.Stored(storage, Wood));
        c.CheckEqual("re-inserted: two disks", 2, storage.Disks.Disks.Count);
        yield return null;

        // Power Rack: one Battery Pack per charged battery.
        c.Loc.debris.Clear();
        c.Require("pickaxe breaks the Power Rack", c.Break(c.T(2, 0), new Pickaxe()));
        yield return Wait.Ticks(2);
        dropped = c.Debris();
        c.CheckEqual("Power Rack drops its 2 Battery Packs", 2, dropped.Where(d => d.QualifiedItemId == O(BatteryPack)).Sum(d => d.Stack));
        c.Check("dropped Power Rack holds no charge", dropped.Where(d => d.QualifiedItemId == BC(Ids.PowerRack)).All(r => !r.modData.ContainsKey(Ids.Data.RackCharge)));

        // Coal Generator: fuel comes back as Coal.
        c.Loc.debris.Clear();
        var gen = c.Place(BC(Ids.CoalGenerator), c.T(5, 0))!;
        c.StandBeside(c.T(5, 0));
        c.Hold(CoalQ, 10);
        c.Action(c.T(5, 0));
        int fuel = RackSlots.GetInt(gen, Ids.Data.Fuel);
        c.CheckEqual("10 Coal fuel the generator", 10 * c.S.Power.Table.CoalPower, fuel);
        c.CheckEqual("all 10 Coal left the backpack", 0, c.Carried(CoalQ));
        c.Require("pickaxe breaks the generator", c.Break(c.T(5, 0), new Pickaxe()));
        yield return Wait.Ticks(2);
        c.CheckEqual("generator drops its 10 Coal", 10, c.Debris().Where(d => d.QualifiedItemId == CoalQ).Sum(d => d.Stack));
    }

    private static IEnumerable<object?> ChestLink(SelfTestContext c)
    {
        c.BuildBasicNetwork();
        var chestTile = c.T(4, 0);
        var chest = c.Place(ChestQ, chestTile) as Chest;
        c.Require("a chest is placed", chest != null);
        c.StandBeside(chestTile);
        c.Hold(O(Ids.ChestLink));
        c.Check("Chest Link: press taken", c.Action(chestTile));
        c.Check("Chest Link: attached", chest!.modData.ContainsKey(Ids.Data.ChestLink));
        c.CheckEqual("Chest Link: used up", 0, c.Carried(O(Ids.ChestLink)));
        var storage = c.StorageAt(c.T(3, 0));
        c.CheckEqual("network has the chest", 1, storage?.Chests.Count ?? 0);
        yield return null;

        // No disks: the Terminal stores into the chest.
        c.Player.Items[3] = ItemRegistry.Create(Stone, 25);
        var menu = c.OpenTerminal(c.T(3, 0));
        c.Require("Terminal opens", menu != null);
        c.ClickInventorySlot(menu!, 3);
        c.CheckEqual("stored into the linked chest", 25, chest.Items.Where(i => i?.QualifiedItemId == Stone).Sum(i => i!.Stack));
        c.CheckEqual("Stone left the backpack", 0, c.Carried(Stone));
        c.ClickStoredItem(menu!, Stone);
        c.CheckEqual("withdrawn from the chest", 25, c.Carried(Stone));
        c.CheckEqual("chest is empty again", 0, chest.Items.Count(i => i != null));
        c.CloseMenu();

        // Wrench takes the Link off and drops it.
        c.StandBeside(chestTile);
        c.Hold(O(Ids.NetworkWrench));
        c.Action(chestTile);
        c.Check("wrench: Link removed", !chest.modData.ContainsKey(Ids.Data.ChestLink));
        yield return Wait.Ticks(1);
        c.CheckEqual("wrench: Link dropped", 1, c.Debris().Count(d => d.QualifiedItemId == O(Ids.ChestLink)));
        c.CheckEqual("network no longer has the chest", 0, c.StorageAt(c.T(3, 0))?.Chests.Count ?? -1);
    }

    private static IEnumerable<object?> Power(SelfTestContext c)
    {
        var (core, _, power, _) = c.BuildBasicNetwork(batteries: 0);
        var n = c.NetworkAt(c.T(0, 0))!;
        c.Check("no batteries: not powered", !c.S.Power.IsPowered(c.Nets, n));

        c.StandBeside(c.T(2, 0));
        c.Hold(O(BatteryPack), 1);
        c.Action(c.T(2, 0));
        c.S.Networks.Invalidate(c.Loc);
        n = c.NetworkAt(c.T(0, 0))!;
        c.Check("one battery: powered", c.S.Power.IsPowered(c.Nets, n), c.S.Power.Describe(c.Nets, n));

        // A Core that ran out this morning comes back when power is added mid-day.
        core.modData[Ids.Data.Powered] = "0";
        c.Check("Core flagged out: not powered", !c.S.Power.IsPowered(c.Nets, n));
        var gen = c.Place(BC(Ids.CoalGenerator), c.T(4, 0))!;
        c.S.Networks.Invalidate(c.Loc);
        c.StandBeside(c.T(4, 0));
        c.Hold(CoalQ, 5);
        c.Action(c.T(4, 0));
        n = c.NetworkAt(c.T(0, 0))!;
        c.Check("fuel added: re-check lifts the outage", c.S.Power.IsPowered(c.Nets, n), c.S.Power.Describe(c.Nets, n));
        c.CheckEqual("fuel recorded", 5 * c.S.Power.Table.CoalPower, RackSlots.GetInt(gen, Ids.Data.Fuel));
        yield return null;

        // A Power Rack off the network: adding a battery says it isn't connected.
        var lone = c.Place(BC(Ids.PowerRack), c.T(8, 3))!;
        c.StandBeside(c.T(8, 3));
        c.Hold(O(BatteryPack));
        Game1.hudMessages.Clear();
        c.Action(c.T(8, 3));
        c.Check("unconnected Power Rack: warns", c.Showed(c.T("power.notConnected")), c.Messages());
    }

    private static IEnumerable<object?> PowerDay(SelfTestContext c)
    {
        var (core, _, power, _) = c.BuildBasicNetwork(batteries: 1);
        var n = c.NetworkAt(c.T(0, 0))!;
        int use = c.S.Power.DailyUse(c.Nets, n);
        int before = RackSlots.GetInts(power, Ids.Data.RackCharge).Sum();
        c.S.Power.OnDayStarted();
        yield return null;
        int after = RackSlots.GetInts(power, Ids.Data.RackCharge).Sum();
        c.CheckEqual("morning pass takes one day's use", before - use, after);
        c.CheckEqual("Core marked powered", "1", core.modData.TryGetValue(Ids.Data.Powered, out string? p) ? p : "(none)");
    }

    private static IEnumerable<object?> MachineLink(SelfTestContext c)
    {
        c.BuildBasicNetwork();
        c.InsertDisk(c.T(1, 0), c.NewDisk());
        var storage = c.StorageAt(c.T(3, 0))!;
        storage.Put(ItemRegistry.Create(CopperOre, 15));
        storage.Put(ItemRegistry.Create(CoalQ, 5));

        var furnaceTile = c.T(4, 0);
        var furnace = c.Place(Furnace, furnaceTile)!;
        c.StandBeside(furnaceTile);
        c.Hold(O(Ids.MachineLink));
        c.Check("Machine Link: press taken", c.Action(furnaceTile));
        c.Check("Machine Link: attached", furnace.modData.ContainsKey(Ids.Data.MachineLink));
        c.S.Networks.Invalidate(c.Loc);

        c.S.Links.OnTimeChanged();
        c.Check("not set up: Furnace stays empty", furnace.heldObject.Value == null);

        MachineLinkService.SetSettings(furnace.modData, new LinkSettings(
            new[] { new InputRule(new ItemKey(CopperOre, 0)) }, Array.Empty<KeepBackRule>(), OutputMode.ToStorage, Schedule.AlwaysOn, true));
        c.S.Networks.Invalidate(c.Loc);
        c.S.Links.OnTimeChanged();
        storage = c.StorageAt(c.T(3, 0))!;
        c.Check("configured: Furnace is smelting", furnace.heldObject.Value != null, furnace.heldObject.Value?.Name ?? "empty");
        c.CheckEqual("configured: 5 Copper Ore taken", 10L, SelfTestContext.Stored(storage, CopperOre));
        c.CheckEqual("configured: 1 Coal taken", 4L, SelfTestContext.Stored(storage, CoalQ));
        yield return null;

        furnace.minutesElapsed(600);
        c.Check("Furnace finished", furnace.readyForHarvest.Value);
        c.S.Links.OnTimeChanged();
        storage = c.StorageAt(c.T(3, 0))!;
        c.CheckEqual("bar collected into storage", 1L, SelfTestContext.Stored(storage, CopperBarQ));
        c.Check("Furnace reloaded", furnace.heldObject.Value != null && !furnace.readyForHarvest.Value);
        c.CheckEqual("more ore used", 5L, SelfTestContext.Stored(storage, CopperOre));
    }

    private static IEnumerable<object?> Autocraft(SelfTestContext c)
    {
        bool learned = !c.Player.craftingRecipes.ContainsKey(Ids.CopperWire);
        if (learned)
            c.Player.craftingRecipes.Add(Ids.CopperWire, 0);
        try
        {
            c.BuildBasicNetwork(batteries: 4);
            c.InsertDisk(c.T(1, 0), c.NewDisk());
            c.Place(BC(Ids.CraftingRack), c.T(4, 0));
            c.S.Networks.Invalidate(c.Loc);
            var storage = c.StorageAt(c.T(3, 0))!;
            storage.Put(ItemRegistry.Create(CopperBarQ, 3));
            storage.Put(ItemRegistry.Create(FiberQ, 3));

            var n = c.NetworkAt(c.T(0, 0))!;
            var target = new ItemKey(O(Ids.CopperWire), 0);
            var plan = c.S.Crafting.Plan(c.Nets, n, target, 20);
            c.Check("plan for 20 Copper Wire can run", plan.CanRun, string.Join(", ", plan.Missing.Select(m => $"{m.Count} {m.Item.QualifiedId}")));
            string? error = c.S.Crafting.Start(c.Nets, n, target, 20);
            c.Require("job starts", error == null, error ?? "");
            yield return null;

            for (int i = 0; i < 6 && SelfTestContext.Stored(c.StorageAt(c.T(3, 0)), O(Ids.CopperWire)) < 20; i++)
            {
                c.S.Networks.Clear();
                c.S.Crafting.OnTimeChanged();
                yield return null;
            }
            storage = c.StorageAt(c.T(3, 0))!;
            c.CheckEqual("20 Copper Wire crafted into storage", 20L, SelfTestContext.Stored(storage, O(Ids.CopperWire)));
            c.CheckEqual("2 Copper Bars used", 1L, SelfTestContext.Stored(storage, CopperBarQ));
            c.CheckEqual("2 Fiber used", 1L, SelfTestContext.Stored(storage, FiberQ));
            var core = c.Nets.ObjectOf(c.NetworkAt(c.T(0, 0))!.Core!);
            c.Check("job finished", Crafting.CraftingService.JobsOf(core).Jobs.All(j => j.Finished));

            // Not enough materials: the plan says what's missing and no job starts.
            var big = c.S.Crafting.Plan(c.Nets, c.NetworkAt(c.T(0, 0))!, target, 1000);
            c.Check("plan for 1000 can't run", !big.CanRun);
            c.Check("start for 1000 refused", c.S.Crafting.Start(c.Nets, c.NetworkAt(c.T(0, 0))!, target, 1000) != null);
        }
        finally
        {
            if (learned)
                c.Player.craftingRecipes.Remove(Ids.CopperWire);
        }
    }

    private static IEnumerable<object?> Tools(SelfTestContext c)
    {
        c.BuildBasicNetwork();
        c.InsertDisk(c.T(1, 0), c.NewDisk());
        var term = c.T(3, 0);
        c.Player.Items[3] = new Pickaxe();
        string id = c.Player.Items[3].QualifiedItemId;
        var menu = c.OpenTerminal(term);
        c.Require("Terminal opens", menu != null);
        c.ClickInventorySlot(menu!, 3);
        c.CheckEqual("Pickaxe left the backpack", 0, c.Carried(id));
        c.Check("Pickaxe is stored", c.StorageAt(term)!.Rows().Any(r => r.Display.QualifiedItemId == id));
        yield return null;
        c.Check("Pickaxe is listed", c.ClickStoredItem(menu!, id));
        c.CheckEqual("Pickaxe withdrawn", 1, c.Carried(id));
        c.Check("Pickaxe is a working tool again", c.CarriedItems(id).FirstOrDefault() is Pickaxe);
        c.CloseMenu();
    }

    private static IEnumerable<object?> Save(SelfTestContext c)
    {
        var (_, rack, power, _) = c.BuildBasicNetwork();
        var disk = c.NewDisk(Ids.IronDisk);
        c.InsertDisk(c.T(1, 0), disk);
        c.StorageAt(c.T(3, 0))!.Put(ItemRegistry.Create(Stone, 40));
        string diskId = SelfTestContext.DiskIdOf(disk);
        var wireTiles = c.S.Wires.For(c.Loc).Tiles.Select(kv => kv.Key).ToHashSet();

        // 1. The mod's own save data, through SMAPI's save-data API and a fresh set of stores.
        if (!StardewModdingAPI.Context.IsMainPlayer)
        {
            c.Check("save data: host only (skipped)", true);
        }
        else
        {
            new SaveCoordinator(c.Helper, c.S.Wires, c.S.Disks).Save();
            var wires2 = new WireLayerStore(new NullMonitor());
            var disks2 = new DiskStore(new NullMonitor());
            new SaveCoordinator(c.Helper, wires2, disks2).Load();
            var layer2 = wires2.Find(c.Loc);
            c.Check("save data: wire tiles round-trip", layer2 != null && layer2.Tiles.Select(kv => kv.Key).ToHashSet().SetEquals(wireTiles), $"{layer2?.Count ?? 0} of {wireTiles.Count}");
            c.CheckEqual("save data: disk contents round-trip", 40L, disks2.Get(diskId, DiskTier.Iron).Count(new ItemKey(Stone, 0)));
            c.CheckEqual("save data: disk tier round-trip", DiskTier.Iron, disks2.Get(diskId, DiskTier.Copper).Tier);
        }
        yield return null;

        // 2. The game's own save format for the location: blocks keep their modData (disks, charge).
        GameLocation? copy = null;
        try
        {
            using var stream = new MemoryStream();
            SaveGame.locationSerializer.Serialize(stream, c.Loc);
            stream.Position = 0;
            copy = (GameLocation?)SaveGame.locationSerializer.Deserialize(stream);
        }
        catch (Exception e)
        {
            c.Check("game save format: location serializes", false, e.GetBaseException().Message);
        }
        if (copy != null)
        {
            c.CheckEqual("game save format: same number of objects", c.Loc.objects.Count(), copy.objects.Count());
            foreach (var (tile, obj) in c.Loc.objects.Pairs)
            {
                bool same = copy.objects.TryGetValue(tile, out var o2) && o2.QualifiedItemId == obj.QualifiedItemId
                    && obj.modData.Pairs.All(kv => o2.modData.TryGetValue(kv.Key, out string? v) && v == kv.Value);
                c.Check($"game save format: {obj.Name} at {c.Rel(tile)} keeps its data", same);
            }
        }
    }

    /// <summary>A monitor that drops everything (for the second set of stores in the save test).</summary>
    private sealed class NullMonitor : StardewModdingAPI.IMonitor
    {
        public bool IsVerbose => false;
        public void Log(string message, StardewModdingAPI.LogLevel level = StardewModdingAPI.LogLevel.Trace) { }
        public void LogOnce(string message, StardewModdingAPI.LogLevel level = StardewModdingAPI.LogLevel.Trace) { }
        public void VerboseLog(string message) { }
        public void VerboseLog(ref StardewModdingAPI.Framework.Logging.VerboseLogStringHandler message) { }
    }
}
