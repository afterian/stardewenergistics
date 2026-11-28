using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Machines;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using static StardewEnergistics.Content.VanillaIds;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// The showcase: builds a whole working base one step at a time, slowly enough to watch, with a screenshot
/// after each step and checks that each step worked. Run with <c>se_selftest showcase</c> or
/// <c>selftest.bat -Showcase</c>.
/// </summary>
internal static class Showcase
{
    private static string O(string id) => "(O)" + id;
    private static string BC(string id) => "(BC)" + id;

    private const string Stone = "(O)390", Wood = "(O)388", CopperOre = "(O)378", CopperBar = "(O)334", Fiber = "(O)771", Coal = "(O)382", Parsnip = "(O)24";

    // Layout (x, y from the area's top-left). Row 0: the computer; row 3: machines and a chest.
    //   0 Core  1 Storage Rack  2 Storage Rack  3 Power Rack  4 Coal Generator  5 Terminal  6 Crafting Rack
    //   wire runs along row 0, down column 7 and along row 3 to the Furnace (x 6) and the chest (x 4).

    public static IEnumerable<object?> Run(SelfTestContext c)
    {
        bool learned = !c.Player.craftingRecipes.ContainsKey(Ids.CopperWire);
        if (learned)
            c.Player.craftingRecipes.Add(Ids.CopperWire, 0);

        c.StandAt(c.T(4, 5), Game1.up);
        yield return c.Show("An empty building, before anything is built", 90);

        // Wire, one tile at a time.
        var wireTiles = c.Row(0, 0, 7).Concat(new[] { c.T(7, 1), c.T(7, 2) }).Concat(c.Row(3, 3, 7).Reverse()).ToList();
        c.Hold(O(Ids.CopperWire), wireTiles.Count + 5);
        foreach (var t in wireTiles)
        {
            c.StandBeside(t);
            c.Hold(O(Ids.CopperWire), wireTiles.Count);
            c.Input.Press(SButton.MouseLeft, t);
            yield return Wait.Ticks(6);
        }
        c.Check("all wire laid", wireTiles.All(t => c.S.Wires.For(c.Loc).Has(SelfTestContext.P(t))));
        c.StandAt(c.T(4, 5), Game1.up);
        yield return c.Show("Copper Wire laid along the wall and over to the machine corner");

        // The computer.
        var blocks = new (string Id, int X, string Name)[]
        {
            (Ids.ComputerCore, 0, "Computer Core"), (Ids.StorageRack, 1, "Storage Rack"), (Ids.StorageRack, 2, "second Storage Rack"),
            (Ids.PowerRack, 3, "Power Rack"), (Ids.CoalGenerator, 4, "Coal Generator"), (Ids.NetworkTerminal, 5, "Network Terminal"),
            (Ids.CraftingRack, 6, "Crafting Rack"),
        };
        foreach (var (id, x, name) in blocks)
        {
            c.Place(BC(id), c.T(x, 0));
            yield return Wait.Ticks(25);
        }
        c.S.Networks.Invalidate(c.Loc);
        var net = c.NetworkAt(c.T(0, 0));
        c.Check("every block is on the Core's network", blocks.All(b => c.NetworkAt(c.T(b.X, 0))?.Id == net?.Id && net?.Core != null));
        c.StandAt(c.T(4, 2), Game1.up);
        yield return c.Show("Core, two Storage Racks, Power Rack, Coal Generator, Terminal and Crafting Rack on the wire");

        // Power.
        for (int i = 0; i < 3; i++)
        {
            c.StandBeside(c.T(3, 0));
            c.Hold(O(BatteryPack));
            c.Action(c.T(3, 0));
            yield return Wait.Ticks(15);
        }
        c.StandBeside(c.T(4, 0));
        c.Hold(Coal, 20);
        c.Action(c.T(4, 0));
        c.S.Networks.Invalidate(c.Loc);
        net = c.NetworkAt(c.T(0, 0))!;
        c.Check("network is powered", c.S.Power.IsPowered(c.Nets, net), c.S.Power.Describe(c.Nets, net));
        yield return c.Show("3 Battery Packs in the Power Rack and 20 Coal in the generator");

        // Core status.
        c.StandBeside(c.T(0, 0));
        c.HoldNothing();
        c.Action(c.T(0, 0));
        yield return c.Show("The Core's status: blocks, disks and power", 100);
        if (Game1.activeClickableMenu != null)
            Game1.exitActiveMenu();
        yield return Wait.Ticks(10);

        // Disks.
        var disks = new[] { c.NewDisk(), c.NewDisk(Ids.IronDisk), c.NewDisk(Ids.GoldDisk) };
        c.InsertDisk(c.T(1, 0), disks[0]);
        yield return Wait.Ticks(20);
        c.InsertDisk(c.T(1, 0), disks[1]);
        yield return Wait.Ticks(20);
        c.InsertDisk(c.T(2, 0), disks[2]);
        c.CheckEqual("network sees 3 disks", 3, c.StorageAt(c.T(5, 0))?.Disks.Disks.Count ?? 0);
        yield return c.Show("Copper, Iron and Gold Disks in the Storage Racks");

        // A linked chest and a Furnace with a Machine Link on the lower row.
        var chest = c.Place("(BC)130", c.T(4, 3)) as Chest;
        c.StandBeside(c.T(4, 3));
        c.Hold(O(Ids.ChestLink));
        c.Action(c.T(4, 3));
        var furnace = c.Place("(BC)13", c.T(6, 3))!;
        c.StandBeside(c.T(6, 3));
        c.Hold(O(Ids.MachineLink));
        c.Action(c.T(6, 3));
        c.S.Networks.Invalidate(c.Loc);
        c.Check("chest is linked", chest?.modData.ContainsKey(Ids.Data.ChestLink) == true);
        c.Check("Furnace has a Machine Link", furnace.modData.ContainsKey(Ids.Data.MachineLink));
        c.StandAt(c.T(5, 5), Game1.up);
        yield return c.Show("A chest with a Chest Link and a Furnace with a Machine Link");

        // Network View (holding the wrench shows all wire).
        c.Hold(O(Ids.NetworkWrench));
        yield return Wait.Ticks(5);
        yield return c.Show("Network View while holding the Network Wrench");

        // Store things through the Terminal.
        c.ClearBackpack();
        c.Player.Items[0] = ItemRegistry.Create(Stone, 300);
        c.Player.Items[1] = ItemRegistry.Create(Wood, 150);
        c.Player.Items[2] = ItemRegistry.Create(CopperOre, 30);
        c.Player.Items[3] = ItemRegistry.Create(Coal, 20);
        c.Player.Items[4] = ItemRegistry.Create(CopperBar, 10);
        c.Player.Items[5] = ItemRegistry.Create(Fiber, 10);
        c.Player.Items[6] = ItemRegistry.Create(Parsnip, 25);
        var menu = c.OpenTerminal(c.T(5, 0));
        c.Require("Terminal opens", menu != null);
        yield return c.Show("The Terminal, with an empty network and a full backpack", 90);
        for (int slot = 0; slot <= 6; slot++)
        {
            c.ClickInventorySlot(menu!, slot);
            yield return Wait.Ticks(12);
        }
        var storage = c.StorageAt(c.T(5, 0))!;
        c.CheckEqual("300 Stone stored", 300L, SelfTestContext.Stored(storage, Stone));
        c.CheckEqual("backpack emptied into storage", 0, c.Player.Items.Take(7).Count(i => i != null));
        yield return c.Show("Everything stored: the backpack is empty and the network holds it all", 90);

        c.ClickStoredItem(menu!, Parsnip);
        yield return Wait.Ticks(10);
        c.CheckEqual("25 Parsnips withdrawn", 25, c.Carried(Parsnip));
        yield return c.Show("Parsnips taken back out with one click");

        // Autocraft screen.
        var auto = c.Helper.Reflection.GetField<ClickableComponent>(menu!, "autoButton").GetValue();
        menu!.receiveLeftClick(auto.bounds.Center.X, auto.bounds.Center.Y);
        yield return Wait.Ticks(10);
        c.Check("Autocraft screen opens", Game1.activeClickableMenu?.GetType().Name == "AutocraftMenu", Game1.activeClickableMenu?.GetType().Name ?? "none");
        yield return c.Show("The Autocraft screen lists what the network can make", 90);
        Game1.exitActiveMenu();
        yield return Wait.Ticks(10);

        // Autocraft 40 Copper Wire.
        net = c.NetworkAt(c.T(0, 0))!;
        var wire = new ItemKey(O(Ids.CopperWire), 0);
        string? error = c.S.Crafting.Start(c.Nets, net, wire, 40);
        c.Check("autocraft job for 40 Copper Wire starts", error == null, error ?? "");
        for (int i = 0; i < 8 && SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), O(Ids.CopperWire)) < 40; i++)
        {
            c.S.Networks.Clear();
            c.S.Crafting.OnTimeChanged();
            yield return Wait.Ticks(20);
        }
        c.CheckEqual("40 Copper Wire crafted", 40L, SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), O(Ids.CopperWire)));
        c.StandAt(c.T(6, 1), Game1.up);
        yield return c.Show("The Crafting Rack made 40 Copper Wire from bars and fiber in storage");

        // The Furnace runs from storage.
        MachineLinkService.SetSettings(furnace.modData, new LinkSettings(
            new[] { new InputRule(new ItemKey(CopperOre, 0)) }, Array.Empty<KeepBackRule>(), OutputMode.ToStorage, Schedule.AlwaysOn, true));
        c.S.Networks.Clear();
        c.S.Links.OnTimeChanged();
        c.Check("Furnace loaded from storage", furnace.heldObject.Value != null);
        c.StandAt(c.T(5, 4), Game1.right);
        yield return c.Show("The Machine Link loaded Copper Ore and Coal into the Furnace");
        long bars = SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), CopperBar);
        furnace.minutesElapsed(600);
        c.S.Networks.Clear();
        c.S.Links.OnTimeChanged();
        c.CheckEqual("Copper Bar collected into storage", bars + 1, SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), CopperBar));
        yield return c.Show("The bar went into storage and the Furnace started the next batch");

        // Final look at the Terminal.
        menu = c.OpenTerminal(c.T(5, 0));
        yield return c.Show("The Terminal now shows the crafted wire and smelted bars", 120);
        if (Game1.activeClickableMenu != null)
            Game1.exitActiveMenu();
        c.StandAt(c.T(4, 5), Game1.up);
        yield return c.Show("The finished base", 150);

        if (learned)
            c.Player.craftingRecipes.Remove(Ids.CopperWire);
    }
}
