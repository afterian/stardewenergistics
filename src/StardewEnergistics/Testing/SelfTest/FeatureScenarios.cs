using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Core.Wires;
using StardewEnergistics.Crafting;
using StardewEnergistics.Machines;
using StardewEnergistics.Storage;
using StardewValley;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>Focused scenarios for single features (the playthrough covers them too, in passing).</summary>
internal static class FeatureScenarios
{
    private static string O(string id) => "(O)" + id;
    private static string BC(string id) => "(BC)" + id;

    private const string CopperBar = "(O)334", GoldBar = "(O)336", IridiumBar = "(O)337", Quartz = "(O)338", Battery = "(O)787", Parsnip = "(O)24";

    /// <summary>All three processors from the Processor Press; a bar without its other inputs is refused with the reason.</summary>
    public static IEnumerable<object?> Press(SelfTestContext c)
    {
        var tile = c.T(0, 0);
        c.Require("Processor Press placed", c.Place(BC(Ids.ProcessorPress), tile) != null);
        int stat = (int)c.Player.stats.Get(Ids.StatCopperProcessors);

        c.Hold(CopperBar, 1);
        bool took = c.GameAction(tile);
        c.Check("a Copper Bar without Refined Quartz is refused", c.Loc.objects[tile].heldObject.Value == null, $"game action returned {took}");
        c.Check("the press says what's missing", Game1.hudMessages.Any(m => m.message.Contains(ItemRegistry.GetData(Quartz)!.DisplayName)), c.Messages());
        c.CheckEqual("the bar stays in the hand", 1, c.Carried(CopperBar));

        c.CheckEqual("Copper Processor pressed", 1, c.PressProcessors(tile, "334", 1));
        c.CheckEqual("pressing counts toward Maru's 10", stat + 1, (int)c.Player.stats.Get(Ids.StatCopperProcessors));
        c.CheckEqual("Gold Processor pressed", 1, c.PressProcessors(tile, "336", 1));
        c.CheckEqual("Iridium Processor pressed", 1, c.PressProcessors(tile, "337", 1));
        c.CheckEqual("the presses used up their other inputs (one of each processor left)", "1 1 0 0",
            $"{c.Carried(O(Ids.CopperProcessor))} {c.Carried(O(Ids.GoldProcessor))} {c.Carried(Quartz)} {c.Carried("(O)72")}");
        if (!c.Throwaway)
            c.Player.stats.Set(Ids.StatCopperProcessors, stat); // console run: leave the player's count as it was
        yield break;
    }

    /// <summary>Heavy Cable lays from the hand, the wrench changes its colour and hides it, a pickaxe picks it up as Heavy Cable.</summary>
    public static IEnumerable<object?> HeavyCable(SelfTestContext c)
    {
        var tiles = c.Row(1, 0, 3).ToList();
        c.CheckEqual("4 tiles of Heavy Cable laid", 4, c.LayWire(tiles, Ids.HeavyCable));
        var layer = c.S.Wires.For(c.Loc);
        c.Check("they're Heavy Cable", tiles.All(t => layer.Get(SelfTestContext.P(t))?.Kind == WireKind.Heavy));

        c.StandAt(c.T(0, 2), Game1.up);
        c.Hold(O(Ids.NetworkWrench));
        int color = layer.Get(SelfTestContext.P(tiles[0]))!.Color;
        c.Action(tiles[0]);
        c.Check("the wrench's action button changes the colour", layer.Get(SelfTestContext.P(tiles[0]))!.Color != color);
        c.UseTool(tiles[1]);
        c.Check("a wrench tap hides one tile", layer.Get(SelfTestContext.P(tiles[1]))!.Covered);
        c.UseTool(tiles[1]);
        c.Check("a second tap shows it again", !layer.Get(SelfTestContext.P(tiles[1]))!.Covered);

        c.StandAt(c.T(3, 2), Game1.up);
        c.Hold(new Pickaxe());
        c.UseTool(tiles[3]);
        c.Check("the pickaxe picks the cable up", !layer.Has(SelfTestContext.P(tiles[3])));
        yield return Wait.Ticks(2);
        c.Check("it drops as Heavy Cable", c.Debris().Any(i => i.QualifiedItemId == O(Ids.HeavyCable)), string.Join(", ", c.Debris().Select(i => i.QualifiedItemId)));
    }

    /// <summary>Crafting an Iron Disk from a Copper Disk at the crafting menu keeps the disk's ID and everything on it.</summary>
    public static IEnumerable<object?> DiskUpgrade(SelfTestContext c)
    {
        c.BigBackpack();
        if (!c.Player.craftingRecipes.ContainsKey(Ids.IronDisk))
            c.Player.craftingRecipes.Add(Ids.IronDisk, 0);
        var (_, rack, _, terminal) = c.BuildBasicNetwork();
        var copper = c.NewDisk();
        string id = SelfTestContext.DiskIdOf(copper);
        c.InsertDisk(rack.TileLocation, copper);
        var storage = c.StorageAt(terminal.TileLocation)!;
        storage.Put(ItemRegistry.Create(Parsnip, 30));
        c.CheckEqual("30 Parsnips on the disk", 30L, SelfTestContext.Stored(storage, Parsnip));

        c.StandBeside(rack.TileLocation);
        c.HoldNothing();
        c.Action(rack.TileLocation);
        c.Require("the disk comes out of the rack", c.Carried(O(Ids.CopperDisk)) == 1, c.Messages());

        var iron = c.Craft(Ids.IronDisk);
        c.CloseMenu();
        c.Require("Iron Disk crafted", iron != null);
        c.CheckEqual("the Copper Disk was used", 0, c.Carried(O(Ids.CopperDisk)));
        c.CheckEqual("the Iron Disk took over the disk's ID", id, SelfTestContext.DiskIdOf(iron!));

        var fromBackpack = c.TakeFromBackpack(O(Ids.IronDisk))!;
        c.InsertDisk(rack.TileLocation, fromBackpack);
        storage = c.StorageAt(terminal.TileLocation)!;
        c.CheckEqual("the Parsnips are still on it", 30L, SelfTestContext.Stored(storage, Parsnip));
        yield break;
    }

    /// <summary>A Shipping Link on a Mini-Shipping Bin ships its items from storage into the farm's Shipping Bin in the evening.</summary>
    public static IEnumerable<object?> ShippingLink(SelfTestContext c)
    {
        var (_, rack, _, terminal) = c.BuildBasicNetwork();
        c.InsertDisk(rack.TileLocation, c.NewDisk());
        var storage = c.StorageAt(terminal.TileLocation)!;
        storage.Put(ItemRegistry.Create(Parsnip, 12));

        var bin = c.Place("(BC)248", c.T(4, 0))!;
        c.StandBeside(c.T(4, 0));
        c.Hold(O(Ids.ShippingLink));
        c.Action(c.T(4, 0));
        c.Require("Shipping Link attached", bin.modData.ContainsKey(Ids.Data.ShippingLink));
        MachineLinkService.SetSettings(bin.modData, new LinkSettings(
            new[] { new InputRule(new ItemKey(Parsnip, 0)) }, System.Array.Empty<KeepBackRule>(), OutputMode.ToStorage, Schedule.AlwaysOn, true));

        var shippingBin = Game1.getFarm().getShippingBin(Game1.MasterPlayer);
        int before = shippingBin.CountId(Parsnip);
        Quest.Evening(c);
        c.CheckEqual("12 Parsnips shipped into the farm's Shipping Bin", before + 12, shippingBin.CountId(Parsnip));
        c.CheckEqual("storage gave them up", 0L, SelfTestContext.Stored(c.StorageAt(terminal.TileLocation), Parsnip));
        shippingBin.ReduceId(Parsnip, 12); // not really selling them
        yield break;
    }

    /// <summary>A Power Feed on a Lightning Rod moves the rod's Battery Pack into the network's Power Rack on the clock.</summary>
    public static IEnumerable<object?> PowerFeed(SelfTestContext c)
    {
        var (_, _, power, _) = c.BuildBasicNetwork(batteries: 1);
        var rod = c.Place("(BC)9", c.T(4, 0))!;
        c.StandBeside(c.T(4, 0));
        c.Hold(O(Ids.PowerFeed));
        c.Action(c.T(4, 0));
        c.Require("Power Feed attached", rod.modData.ContainsKey(Ids.Data.PowerFeed));

        c.StandBeside(c.T(0, 0));
        c.Hold(O(Ids.PowerFeed));
        c.Action(c.T(0, 0));
        c.Check("a Power Feed on anything else is refused", c.Showed(c.T("feed.notTarget")), c.Messages());

        rod.heldObject.Value = ItemRegistry.Create<SObject>(Battery);
        rod.MinutesUntilReady = 0;
        rod.readyForHarvest.Value = true;
        foreach (object? s in c.TimePasses()) yield return s;
        c.CheckEqual("the rod's Battery Pack went into the Power Rack", 2, RackSlots.GetInts(power, Ids.Data.RackCharge).Count);
        c.Check("the rod is empty", rod.heldObject.Value == null);

        c.StandBeside(c.T(4, 0));
        c.Hold(O(Ids.NetworkWrench));
        c.Action(c.T(4, 0));
        c.Check("the wrench takes the Power Feed off", !rod.modData.ContainsKey(Ids.Data.PowerFeed));
    }

    /// <summary>The Wireless Terminal opens the network from anywhere in the building; with no network it says so.</summary>
    public static IEnumerable<object?> Wireless(SelfTestContext c)
    {
        c.StandAt(c.T(5, 4), Game1.up);
        c.Hold(O(Ids.WirelessTerminal));
        c.Input.Press(StardewModdingAPI.SButton.MouseRight, c.T(5, 3), c.T(5, 3));
        c.Check("with no network nearby, it says there's none", Game1.activeClickableMenu == null && c.Showed(c.T("wireless.none")), c.Messages());
        c.CloseMenu();

        c.BuildBasicNetwork();
        c.StandAt(c.T(8, 5), Game1.up);
        c.Hold(O(Ids.WirelessTerminal));
        c.Input.Press(StardewModdingAPI.SButton.MouseRight, c.T(8, 4), c.T(8, 4));
        c.Check("the Wireless Terminal opens the Terminal screen", Game1.activeClickableMenu?.GetType().Name == "TerminalMenu", Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        c.CloseMenu();
        yield break;
    }

    /// <summary>The Advanced Crafting Rack's keep-in-stock rule starts a job overnight and tops the item up on the clock.</summary>
    public static IEnumerable<object?> KeepInStock(SelfTestContext c)
    {
        var (core, rack, _, terminal) = c.BuildBasicNetwork(batteries: 3);
        c.InsertDisk(rack.TileLocation, c.NewDisk());
        c.Place(BC(Ids.AdvancedCraftingRack), c.T(4, 0));
        c.S.Networks.Invalidate(c.Loc);
        var storage = c.StorageAt(terminal.TileLocation)!;
        storage.Put(ItemRegistry.Create(CopperBar, 5));
        storage.Put(ItemRegistry.Create("(O)771", 5));
        var net = c.NetworkAt(c.T(0, 0))!;

        c.Check("keep-in-stock rule for 30 Copper Wire is set", c.S.Crafting.SetStockRule(c.Nets, net, new ItemKey(O(Ids.CopperWire), 0), 30) == null);
        c.CheckEqual("the rule is saved on the rack", 1, CraftingService.StockRulesOf(c.Nets, net).Count);
        Quest.Evening(c);
        c.Check("overnight, a job starts for the missing wire", CraftingService.JobsOf(core).Jobs.Count > 0);
        for (int i = 0; i < 12 && SelfTestContext.Stored(c.StorageAt(terminal.TileLocation), O(Ids.CopperWire)) < 30; i++)
            foreach (object? s in c.TimePasses()) yield return s;
        c.CheckEqual("30 Copper Wire in stock", 30L, SelfTestContext.Stored(c.StorageAt(terminal.TileLocation), O(Ids.CopperWire)));
    }
}
