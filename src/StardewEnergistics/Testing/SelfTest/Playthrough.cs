using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Guide;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Progression;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Crafting;
using StardewEnergistics.Machines;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// The whole mod played from a new farmer to the journal's last page, the way a player would: Maru's letters
/// as hearts and progress grow, every recipe crafted at the game's crafting menu, processors pressed at the
/// Processor Press, the base built tile by tile, Links and Feeds attached by hand, buildings joined with
/// Network Ports on the real farm, and Ginger Island linked with an Uplink. Each tier's letter is first
/// checked to wait for its last missing requirement; a letter that never comes stops the run and says which
/// requirement is short ("progression blocked").
/// <para>
/// Time, hearts and mine depth are set (a real season would take hours); everything else goes through the
/// game's and the mod's own code. Runs as <c>playthrough</c> (fast) and <c>showcase</c> (slow, with screenshots).
/// It changes the farm and the player, so it only runs in a session launched for the self-test.
/// </para>
/// </summary>
internal static class Playthrough
{
    private static string O(string id) => "(O)" + id;
    private static string BC(string id) => "(BC)" + id;

    private const string CopperBar = "(O)334", IronBar = "(O)335", GoldBar = "(O)336", IridiumBar = "(O)337", Quartz = "(O)338";
    private const string Coal = "(O)382", CopperOre = "(O)378", Fiber = "(O)771", Battery = "(O)787", PrismaticShard = "(O)74";
    private const string Stone = "(O)390", Wood = "(O)388", Parsnip = "(O)24";
    private const string Furnace = "(BC)13", ChestQ = "(BC)130", MiniBin = "(BC)248", LightningRod = "(BC)9";

    /// <summary>Ten kinds for the "ten kinds stored" goal (with Parsnips to take back out).</summary>
    private static readonly (string Id, int Count)[] TenKinds =
    {
        (Stone, 300), (Wood, 150), ("(O)330", 40), (CopperOre, 30), (Coal, 20), ("(O)380", 20), ("(O)384", 10), (Fiber, 30), ("(O)92", 15), (Parsnip, 25),
    };

    // Layout in the scratch building (x, y from the area's top-left):
    //   row 0: Core 0, Storage Rack 1 and 2, Power Rack 3, Coal Generator 4, Terminal 5, Crafting Rack 6,
    //          Advanced Crafting Rack 7, Island Uplink 8; wire under all of it,
    //   down column 7 to row 3, where it runs x 2..7: Mini-Shipping Bin 2, Lightning Rod 3, chest 4, Furnace 6.
    //   Processor Press at (0,3), off the network. Heavy Cable on row 5.
    private static readonly (int X, int Y) PressAt = (0, 3);

    public static IEnumerable<object?> Run(SelfTestContext c)
    {
        c.BigBackpack();
        foreach (object? s in Prologue(c)) yield return s;
        foreach (object? s in Components(c)) yield return s;
        foreach (object? s in FirstNetwork(c)) yield return s;
        foreach (object? s in Automation(c)) yield return s;
        foreach (object? s in Autocrafting(c)) yield return s;
        foreach (object? s in LinkedBuildings(c)) yield return s;
        foreach (object? s in WholeFarm(c)) yield return s;
        foreach (object? s in Finale(c)) yield return s;
    }

    // ---- Gates and chapters ----

    /// <summary>Mornings run the day's power use too, as a real run of days would.</summary>
    private const bool PowerPass = true;

    private static string Gates(SelfTestContext c, TierId tier) =>
        string.Join(", ", GateRequirements.For(tier, c.S.Progression.Snapshot(c.Player)).Select(r => $"{r.Key} {r.Have}/{r.Need}{(r.Met ? "" : " (short)")}"))
        + "; tiers built: " + string.Join(",", Progression.ProgressionService.TiersBuilt().OrderBy(t => t).Select(t => ((int)t).ToString()));

    /// <summary>A morning passes and Maru's letter for <paramref name="tier"/> must not come yet.</summary>
    private static void NoLetterYet(SelfTestContext c, TierId tier, string because)
    {
        Quest.Morning(c, PowerPass);
        c.Check($"{tier} letter waits while {because}", !Quest.LetterCame(c, tier), Gates(c, tier));
    }

    /// <summary>A morning passes, Maru's letter for <paramref name="tier"/> arrives, is read at the mailbox and teaches its recipes.</summary>
    private static IEnumerable<object?> LetterArrives(SelfTestContext c, TierId tier, string caption)
    {
        Quest.Morning(c, PowerPass);
        c.Require($"PROGRESSION: {tier} letter arrives", c.Player.mailbox.Contains(ContentInjector.MailId(tier)) || c.Player.mailReceived.Contains(ContentInjector.MailId(tier)),
            "progression blocked: " + Gates(c, tier));
        if (c.Player.mailbox.Contains(ContentInjector.MailId(tier)))
            Quest.ReadMail(c, ContentInjector.MailId(tier));
        Quest.CheckRecipes(c, tier);
        yield return c.Show(caption, 110, 40);
        c.CloseMenu();
    }

    /// <summary>
    /// The journal re-checks on the clock; every goal of the chapter must be ticked. Then Maru's thank-you
    /// letter comes the next morning with the chapter's reward.
    /// </summary>
    private static IEnumerable<object?> ChapterDone(SelfTestContext c, int number)
    {
        foreach (object? s in c.TimePasses(2)) yield return s;
        var chapter = GuideBook.Chapters[number];
        var done = Quest.Done(c);
        foreach (var q in chapter.Quests)
            c.Check($"journal chapter {number}: '{q.Id}' ticked", done.Contains(q.Id), Why(c, q));
        var missing = chapter.Quests.Where(q => !done.Contains(q.Id)).Select(q => q.Id).ToList();
        c.Check($"PROGRESSION: journal chapter {number} complete", missing.Count == 0, "missing: " + string.Join(", ", missing));

        c.Step($"open the journal after chapter {number}");
        c.S.Guide.Open();
        yield return c.Show($"The journal after chapter {number}: {done.Count} of {GuideBook.AllQuests.Count()} goals ticked", 110);
        c.CloseMenu();

        if (chapter.RewardItem == null)
            yield break;
        string mail = ContentInjector.GuideChapterMailId(number);
        c.Check($"chapter {number}: Maru's thank-you letter is on its way", c.Player.hasOrWillReceiveMail(mail));
        Quest.Morning(c, PowerPass);
        if (!c.Check($"chapter {number}: thank-you letter arrives", c.Player.mailbox.Contains(mail), string.Join(", ", c.Player.mailbox)))
            yield break;
        string reward = "(O)" + chapter.RewardItem;
        int before = c.Carried(reward);
        Quest.ReadMail(c, mail);
        c.CheckEqual($"chapter {number} reward: {chapter.RewardCount} x {ItemRegistry.GetData(reward)?.DisplayName}", before + chapter.RewardCount, c.Carried(reward));
        yield return c.Show($"Maru's thank-you letter for chapter {number}, with {chapter.RewardCount} {ItemRegistry.GetData(reward)?.DisplayName}", 90, 40);
        c.CloseMenu();
    }

    /// <summary>Why a goal isn't ticked: the facts it depends on.</summary>
    private static string Why(SelfTestContext c, GuideQuest q)
    {
        string world = c.Helper.Reflection.GetMethod(typeof(Guide.GuideService), "WorldFlags").Invoke<HashSet<WorldFlag>>() is { } f ? string.Join(",", f) : "?";
        return q.Goal.Kind switch
        {
            GoalKind.World => $"needs world flag {q.Goal.Flag}; farm has: {world}",
            GoalKind.Get => $"needs {q.Goal.Item} crafted or carried",
            GoalKind.ReadLetter => $"needs letter {q.Goal.Tier} read",
            _ => q.Goal.Kind.ToString(),
        };
    }

    // ---- Prologue: Maru's journal ----

    private static IEnumerable<object?> Prologue(SelfTestContext c)
    {
        var p = c.Player;
        Quest.FreshStart(c);
        c.Check("fresh start: no mod recipes known", !ContentInjector.AllRecipes.Any(p.craftingRecipes.ContainsKey));
        c.StandAt(c.T(4, 5), Game1.up);
        yield return c.Show("Day one: an empty building and a new farmer", 90);

        Quest.Morning(c, PowerPass);
        c.Check("day one: journal recipe known", p.craftingRecipes.ContainsKey(Ids.QuestJournal));
        c.Check("day one: no blueprints yet", !Quest.LetterCame(c, TierId.T1Components));
        Quest.ReadMail(c, ContentInjector.GuideMailId);
        c.CheckEqual("the journal came with Maru's letter", 1, c.Carried(O(Ids.QuestJournal)));
        yield return c.Show("Maru's first letter, with the Tinkerer's Journal", 110, 40);
        c.CloseMenu();

        int slot = c.SlotOf(O(Ids.QuestJournal));
        if (slot >= 0)
        {
            p.CurrentToolIndex = slot;
            c.Step("use the journal");
            c.Input.Press(SButton.MouseRight, p.GetGrabTile());
        }
        c.Check("using the journal opens it", Game1.activeClickableMenu?.GetType().Name == "GuideMenu", Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        yield return c.Show("The Tinkerer's Journal, open on its first page", 110);
        c.CloseMenu();

        Quest.SetMaruHearts(c, 1);
        NoLetterYet(c, TierId.T1Components, "Maru is at 1 heart");
        Quest.SetMaruHearts(c, 2);
        foreach (object? s in LetterArrives(c, TierId.T1Components, "2 hearts with Maru: her first blueprints")) yield return s;
        foreach (object? s in ChapterDone(c, 0)) yield return s;
    }

    // ---- Chapter 1: components ----

    private static IEnumerable<object?> Components(SelfTestContext c)
    {
        var p = c.Player;
        c.Craft(Ids.ProcessorPress);
        yield return c.Show("The crafting menu: Maru's blueprints are there to craft", 90, 30);
        c.CloseMenu();
        var pressTile = c.T(PressAt.X, PressAt.Y);
        c.Require("Processor Press placed", c.Place(BC(Ids.ProcessorPress), pressTile) != null);
        c.CheckEqual("the crafted press was used", 0, c.Carried(BC(Ids.ProcessorPress)));

        c.CheckEqual("first Copper Processor pressed", 1, c.PressProcessors(pressTile, "334", 1));
        c.CheckEqual("processor count is 1", 1, (int)p.stats.Get(Ids.StatCopperProcessors));
        c.StandBeside(pressTile);
        yield return c.Show("The Processor Press makes a Copper Processor from a Copper Bar and Refined Quartz");

        c.Craft(Ids.CopperWire);
        c.CloseMenu();
        c.CheckEqual("Copper Wire comes in tens", 10, c.Carried(O(Ids.CopperWire)));

        p.deepestMineLevel = 20;
        NoLetterYet(c, TierId.T2SimpleStorage, "only 1 of 10 Copper Processors is pressed");
        c.PressProcessors(pressTile, "334", 9);
        c.CheckEqual("processor count is 10", 10, (int)p.stats.Get(Ids.StatCopperProcessors));
        p.deepestMineLevel = 19;
        NoLetterYet(c, TierId.T2SimpleStorage, "the Mines are only at floor 19 of 20");
        p.deepestMineLevel = 20;
        foreach (object? s in ChapterDone(c, 1)) yield return s;
        foreach (object? s in LetterArrives(c, TierId.T2SimpleStorage, "10 processors and Mines floor 20: storage blueprints")) yield return s;
    }

    // ---- Chapter 2: the first network ----

    private static IEnumerable<object?> FirstNetwork(SelfTestContext c)
    {
        Quest.SetMaruHearts(c, 4);
        c.Player.deepestMineLevel = 80;
        NoLetterYet(c, TierId.T3Automation, "no storage network is built yet");

        foreach (var (id, times) in new[] { (Ids.ComputerCore, 1), (Ids.StorageRack, 2), (Ids.PowerRack, 1), (Ids.CoalGenerator, 1),
                     (Ids.NetworkTerminal, 1), (Ids.ChestLink, 1), (Ids.CopperDisk, 1), (Ids.NetworkWrench, 1), (Ids.CopperWire, 2) })
            c.CraftTimes(id, times);

        // Wire, one tile at a time, from the crafted spools.
        var wireTiles = c.Row(0, 0, 8).Concat(new[] { c.T(7, 1), c.T(7, 2) }).Concat(c.Row(3, 2, 7).Reverse()).ToList();
        c.Remove(O(Ids.CopperWire), c.Carried(O(Ids.CopperWire)));
        c.Hold(O(Ids.CopperWire), wireTiles.Count + 10);
        foreach (var t in wireTiles)
        {
            c.StandBeside(t);
            c.Input.Press(SButton.MouseLeft, t);
            yield return Wait.Ticks(c.Slow ? 6 : 0);
        }
        c.Check("all wire laid", wireTiles.All(t => c.S.Wires.For(c.Loc).Has(SelfTestContext.P(t))));
        c.StandAt(c.T(4, 5), Game1.up);
        yield return c.Show("Copper Wire laid along the wall and down to the machine corner");

        foreach (var (id, x) in new[] { (Ids.ComputerCore, 0), (Ids.StorageRack, 1), (Ids.StorageRack, 2), (Ids.PowerRack, 3), (Ids.CoalGenerator, 4), (Ids.NetworkTerminal, 5) })
        {
            c.Place(BC(id), c.T(x, 0));
            yield return Wait.Ticks(c.Slow ? 25 : 0);
        }
        c.CheckEqual("the crafted blocks were used", 0, new[] { Ids.ComputerCore, Ids.StorageRack, Ids.PowerRack, Ids.CoalGenerator, Ids.NetworkTerminal }.Sum(id => c.Carried(BC(id))));
        c.S.Networks.Invalidate(c.Loc);
        var net = c.NetworkAt(c.T(0, 0));
        c.Check("every block is on the Core's network", Enumerable.Range(0, 6).All(x => c.NetworkAt(c.T(x, 0))?.Id == net?.Id) && net?.Core != null);
        c.StandAt(c.T(4, 2), Game1.up);
        yield return c.Show("Core, two Storage Racks, Power Rack, Coal Generator and Terminal on the wire");

        for (int i = 0; i < 3; i++)
        {
            c.StandBeside(c.T(3, 0));
            c.Hold(Battery);
            c.Action(c.T(3, 0));
        }
        c.StandBeside(c.T(4, 0));
        c.Hold(Coal, 20);
        c.Action(c.T(4, 0));
        c.S.Networks.Invalidate(c.Loc);
        net = c.NetworkAt(c.T(0, 0))!;
        c.Check("network is powered", c.S.Power.IsPowered(c.Nets, net), c.S.Power.Describe(c.Nets, net));
        yield return c.Show("3 Battery Packs in the Power Rack and 20 Coal in the generator");

        c.StandBeside(c.T(0, 0));
        c.HoldNothing();
        c.Action(c.T(0, 0));
        c.Check("Core status shows", Game1.activeClickableMenu is DialogueBox, Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        yield return c.Show("The Core's status: blocks, disks and power", 100, 45);
        c.CloseMenu();

        var disk = c.TakeFromBackpack(O(Ids.CopperDisk));
        c.Require("crafted Copper Disk in the backpack", disk != null);
        c.DiskIds.Add(c.S.Disks.ForItem(disk!).Id.Value);
        c.InsertDisk(c.T(1, 0), disk!);
        c.CheckEqual("the network sees the disk", 1, c.StorageAt(c.T(5, 0))?.Disks.Disks.Count ?? 0);

        var chest = c.Place(ChestQ, c.T(4, 3));
        c.StandBeside(c.T(4, 3));
        c.HoldFromBackpack(O(Ids.ChestLink));
        c.Action(c.T(4, 3));
        c.Check("chest is linked", chest?.modData.ContainsKey(Ids.Data.ChestLink) == true);

        // Ten kinds through the Terminal.
        foreach (var (id, count) in TenKinds)
            c.Give(id, count);
        var menu = c.OpenTerminal(c.T(5, 0));
        c.Require("Terminal opens", menu != null);
        yield return c.Show("The Terminal, with the backpack full of things to store", 90);
        foreach (var (id, _) in TenKinds)
        {
            int slot = c.SlotOf(id);
            if (slot >= 0)
                c.ClickInventorySlot(menu!, slot);
            yield return Wait.Ticks(c.Slow ? 8 : 0);
        }
        var storage = c.StorageAt(c.T(5, 0))!;
        c.CheckEqual("ten kinds stored", 10, TenKinds.Count(k => SelfTestContext.Stored(storage, k.Id) > 0));
        yield return c.Show("Everything stored: ten kinds on the network", 90);
        c.ClickStoredItem(menu!, Parsnip);
        c.CheckEqual("25 Parsnips withdrawn", 25, c.Carried(Parsnip));
        yield return c.Show("Parsnips taken back out with one click");
        c.CloseMenu();

        // Hide a wire with the wrench.
        c.StandBeside(c.T(7, 1));
        c.HoldFromBackpack(O(Ids.NetworkWrench));
        c.UseTool(c.T(7, 1));
        c.Check("wrench hides the wire tile", c.S.Wires.For(c.Loc).Get(SelfTestContext.P(c.T(7, 1)))?.Covered == true);
        c.StandAt(c.T(4, 5), Game1.up);
        yield return Wait.Ticks(5);
        yield return c.Show("Network View while holding the Network Wrench (one tile hidden)");

        Quest.SetMaruHearts(c, 3);
        foreach (object? s in ChapterDone(c, 2)) yield return s;
        c.Check($"{TierId.T3Automation} letter waits while Maru is at 3 hearts", !Quest.LetterCame(c, TierId.T3Automation), Gates(c, TierId.T3Automation));
        Quest.SetMaruHearts(c, 4);
        foreach (object? s in LetterArrives(c, TierId.T3Automation, "A working storage network, Mines 80, 4 hearts: automation blueprints")) yield return s;
    }

    // ---- Chapter 3: automation ----

    private static IEnumerable<object?> Automation(SelfTestContext c)
    {
        Quest.SetMaruHearts(c, 6);
        c.Player.deepestMineLevel = 120;
        NoLetterYet(c, TierId.T4Autocrafting, "no Machine Link is set up yet");

        var pressTile = c.T(PressAt.X, PressAt.Y);
        c.CheckEqual("Gold Processor pressed", 1, c.PressProcessors(pressTile, "336", 1));

        // Machine Link on a Furnace, set up with the wrench.
        c.CraftTimes(Ids.MachineLink, 1);
        var furnace = c.Place(Furnace, c.T(6, 3))!;
        c.StandBeside(c.T(6, 3));
        c.HoldFromBackpack(O(Ids.MachineLink));
        c.Action(c.T(6, 3));
        c.Check("Furnace has a Machine Link", furnace.modData.ContainsKey(Ids.Data.MachineLink));
        c.HoldFromBackpack(O(Ids.NetworkWrench));
        c.Action(c.T(6, 3));
        c.Check("the wrench opens the Link's settings", Game1.activeClickableMenu?.GetType().Name == "LinkSettingsMenu", Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        yield return c.Show("The Machine Link's settings, opened with the wrench", 100);
        c.CloseMenu();
        MachineLinkService.SetSettings(furnace.modData, new LinkSettings(
            new[] { new InputRule(new ItemKey(CopperOre, 0)) }, Array.Empty<KeepBackRule>(), OutputMode.ToStorage, Schedule.AlwaysOn, true));
        foreach (object? s in c.TimePasses()) yield return s;
        c.Check("the Link loads the Furnace from storage", furnace.heldObject.Value != null, $"state {furnace.modData.GetValueOrDefault(Ids.Data.LinkState)}");
        c.StandAt(c.T(5, 4), Game1.right);
        yield return c.Show("The Machine Link loaded Copper Ore and Coal into the Furnace");

        // Shipping Link on a Mini-Shipping Bin.
        c.CraftTimes(Ids.ShippingLink, 1);
        var bin = c.Place(MiniBin, c.T(2, 3));
        c.StandBeside(c.T(2, 3));
        c.HoldFromBackpack(O(Ids.ShippingLink));
        c.Action(c.T(2, 3));
        c.Check("Mini-Shipping Bin has a Shipping Link", bin?.modData.ContainsKey(Ids.Data.ShippingLink) == true);

        // Heavy Cable: lay it and change its colour with the wrench.
        c.CraftTimes(Ids.HeavyCable, 1);
        c.CheckEqual("Heavy Cable comes in tens", 10, c.Carried(O(Ids.HeavyCable)));
        c.LayWire(c.Row(5, 0, 2), Ids.HeavyCable);
        var layer = c.S.Wires.For(c.Loc);
        c.Check("Heavy Cable laid", c.Row(5, 0, 2).All(t => layer.Get(SelfTestContext.P(t))?.Kind == Core.Wires.WireKind.Heavy));
        int color = layer.Get(SelfTestContext.P(c.T(0, 5)))?.Color ?? -1;
        c.StandAt(c.T(0, 4), Game1.down);
        c.HoldFromBackpack(O(Ids.NetworkWrench));
        c.Action(c.T(0, 5));
        c.Check("the wrench changes a cable's colour", layer.Get(SelfTestContext.P(c.T(0, 5)))?.Color != color);
        yield return c.Show("A Mini-Shipping Bin with a Shipping Link, and Heavy Cable in a new colour");

        // Disk upgrades: Copper to Iron to Gold, keeping the disk.
        c.CraftTimes(Ids.CopperDisk, 1);
        c.CraftTimes(Ids.IronDisk, 1);
        c.CheckEqual("the Iron Disk used the Copper Disk", 0, c.Carried(O(Ids.CopperDisk)));
        c.CraftTimes(Ids.GoldDisk, 1);
        c.CheckEqual("the Gold Disk used the Iron Disk", 0, c.Carried(O(Ids.IronDisk)));
        var gold = c.TakeFromBackpack(O(Ids.GoldDisk));
        c.Require("crafted Gold Disk in the backpack", gold != null);
        c.DiskIds.Add(c.S.Disks.ForItem(gold!).Id.Value);
        c.InsertDisk(c.T(2, 0), gold!);

        foreach (object? s in ChapterDone(c, 3)) yield return s;
        foreach (object? s in LetterArrives(c, TierId.T4Autocrafting, "A Machine Link at work, Mines 120, 6 hearts: autocrafting blueprints")) yield return s;
    }

    // ---- Chapter 4: autocrafting ----

    private static IEnumerable<object?> Autocrafting(SelfTestContext c)
    {
        Quest.SetMaruHearts(c, 8);
        MineShaft.lowestLevelReached = 145; // Skull Cavern floor 25
        c.Give(IridiumBar);
        NoLetterYet(c, TierId.T5LinkedBuildings, "no Crafting Rack is running yet");

        c.CraftTimes(Ids.CraftingRack, 1);
        c.Place(BC(Ids.CraftingRack), c.T(6, 0));
        c.S.Networks.Invalidate(c.Loc);

        c.Give(CopperBar, 10);
        c.Give(Fiber, 10);
        var menu = c.OpenTerminal(c.T(5, 0));
        c.Require("Terminal opens", menu != null);
        foreach (string id in new[] { CopperBar, Fiber })
            if (c.SlotOf(id) is >= 0 and var slot)
                c.ClickInventorySlot(menu!, slot);
        var auto = c.Helper.Reflection.GetField<ClickableComponent>(menu!, "autoButton").GetValue();
        menu!.receiveLeftClick(auto.bounds.Center.X, auto.bounds.Center.Y);
        c.Check("Autocraft screen opens", Game1.activeClickableMenu?.GetType().Name == "AutocraftMenu", Game1.activeClickableMenu?.GetType().Name ?? "none");
        yield return c.Show("The Autocraft screen lists what the network can make", 90);
        c.CloseMenu();

        var net = c.NetworkAt(c.T(0, 0))!;
        string? error = c.S.Crafting.Start(c.Nets, net, new ItemKey(O(Ids.CopperWire), 0), 40);
        c.Check("autocraft job for 40 Copper Wire starts", error == null, error ?? "");
        for (int i = 0; i < 12 && SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), O(Ids.CopperWire)) < 40; i++)
            foreach (object? s in c.TimePasses()) yield return s;
        c.CheckEqual("40 Copper Wire autocrafted", 40L, SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), O(Ids.CopperWire)));
        c.StandAt(c.T(6, 1), Game1.up);
        yield return c.Show("The Crafting Rack made 40 Copper Wire from bars and fiber in storage");

        c.Remove(IridiumBar, c.Carried(IridiumBar));
        foreach (object? s in ChapterDone(c, 4)) yield return s;
        c.Check($"{TierId.T5LinkedBuildings} letter waits while there's no Iridium Bar in the backpack", !Quest.LetterCame(c, TierId.T5LinkedBuildings), Gates(c, TierId.T5LinkedBuildings));
        c.Give(IridiumBar);
        foreach (object? s in LetterArrives(c, TierId.T5LinkedBuildings, "Autocrafting, Skull Cavern 25, an Iridium Bar, 8 hearts: linked-building blueprints")) yield return s;
    }

    // ---- Chapter 5: linked buildings ----

    private static IEnumerable<object?> LinkedBuildings(SelfTestContext c)
    {
        var p = c.Player;
        if (!p.mailReceived.Contains("Visited_Island"))
            p.mailReceived.Add("Visited_Island");
        Game1.netWorldState.Value.GoldenWalnutsFound = 100;
        c.Give(PrismaticShard);
        NoLetterYet(c, TierId.T6WholeFarm, "no buildings are linked yet");

        var pressTile = c.T(PressAt.X, PressAt.Y);
        c.CheckEqual("Iridium Processor pressed", 1, c.PressProcessors(pressTile, "337", 1));

        // Wireless Terminal.
        c.CraftTimes(Ids.WirelessTerminal, 1);
        c.StandAt(c.T(4, 5), Game1.up);
        c.HoldFromBackpack(O(Ids.WirelessTerminal));
        c.Input.Press(SButton.MouseRight, c.T(4, 4), c.T(4, 4));
        c.Check("the Wireless Terminal opens the network", Game1.activeClickableMenu?.GetType().Name == "TerminalMenu", Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        yield return c.Show("The Wireless Terminal opens the network from anywhere in the building", 90);
        c.CloseMenu();

        // Power Feed on a Lightning Rod; a struck rod's battery goes into the Power Rack.
        c.CraftTimes(Ids.PowerFeed, 1);
        var rod = c.Place(LightningRod, c.T(3, 3))!;
        c.StandBeside(c.T(3, 3));
        c.HoldFromBackpack(O(Ids.PowerFeed));
        c.Action(c.T(3, 3));
        c.Check("Lightning Rod has a Power Feed", rod.modData.ContainsKey(Ids.Data.PowerFeed));
        var rack = c.Loc.objects[c.T(3, 0)];
        int batteries = RackSlots.GetInts(rack, Ids.Data.RackCharge).Count;
        rod.heldObject.Value = ItemRegistry.Create<SObject>(Battery);
        rod.MinutesUntilReady = 0;
        rod.readyForHarvest.Value = true;
        foreach (object? s in c.TimePasses()) yield return s;
        c.CheckEqual("the Power Feed put the rod's Battery Pack in the Power Rack", batteries + 1, RackSlots.GetInts(rack, Ids.Data.RackCharge).Count);
        c.Check("the rod is empty again", rod.heldObject.Value == null);

        c.CraftTimes(Ids.IridiumDisk, 1);

        // Network Ports: the farmhouse and two more buildings, joined by wire across the farm.
        foreach (var (id, times) in new[] { (Ids.NetworkPort, 3), (Ids.ComputerCore, 3), (Ids.PowerRack, 3) })
            c.CraftTimes(id, times);
        var farm = Game1.getFarm();
        var buildings = new List<Building> { farm.GetMainFarmHouse() };
        for (int i = 0; i < 2; i++)
        {
            c.Step("put up a Shed on the farm");
            if (c.BuildOnFarm("Shed") is { } shed)
                buildings.Add(shed);
        }
        if (buildings.Count < 3 && farm.buildings.FirstOrDefault(b => b.buildingType.Value == "Greenhouse") is { } greenhouse)
            buildings.Add(greenhouse);
        c.Require("three buildings to link", buildings.Count >= 3 && buildings.All(b => b.GetIndoors() != null),
            string.Join(", ", buildings.Select(b => $"{b.buildingType.Value} indoors={b.GetIndoors()?.NameOrUniqueName ?? "none"}")));

        // Outside: a Port beside each building, wire between them.
        var own = new HashSet<Vector2>();
        var ports = new List<Vector2>();
        foreach (var b in buildings.Take(3))
        {
            var tile = c.FreeTileBeside(farm, b);
            c.Require($"a free tile beside the {b.buildingType.Value} for its Port", tile != null);
            ports.Add(tile!.Value);
            own.Add(tile.Value);
        }
        foreach (object? s in c.Visit(farm, Vector2.Zero, ports[0] + new Vector2(0, 1))) yield return s;
        foreach (var t in ports)
            c.Require($"Port placed at {t}", c.Place(BC(Ids.NetworkPort), t) != null);
        var wire = new List<Vector2>();
        for (int i = 0; i + 1 < ports.Count; i++)
        {
            var path = c.FindPath(farm, ports[i], ports[i + 1], own);
            c.Require($"a clear path for wire from Port {i + 1} to Port {i + 2}", path != null, $"{ports[i]} to {ports[i + 1]}");
            foreach (var t in path!)
            {
                own.Add(t);
                if (!wire.Contains(t))
                    wire.Add(t);
            }
        }
        c.Step($"lay {wire.Count} tiles of wire between the Ports");
        c.Hold(O(Ids.CopperWire), wire.Count + 10);
        foreach (var t in wire)
        {
            c.StandBeside(t);
            c.Hold(O(Ids.CopperWire), wire.Count + 10);
            c.Input.Press(SButton.MouseLeft, t);
        }
        c.Check("wire laid between the Ports", wire.All(t => c.S.Wires.For(farm).Has(SelfTestContext.P(t))));
        c.S.Networks.Clear();
        c.StandAt(ports[0] + new Vector2(0, 1), Game1.up);
        c.HoldFromBackpack(O(Ids.NetworkWrench));
        yield return Wait.Ticks(5);
        yield return c.Show("Network Ports beside the farmhouse and two Sheds, wired together across the farm", 110);

        // Inside each building: a Core and a charged Power Rack on wire.
        foreach (var b in buildings.Take(3))
        {
            var inside = b.GetIndoors();
            var row = c.FindFreeRow(inside, 3);
            c.Require($"free floor in {inside.NameOrUniqueName} for a Core", row != null);
            foreach (object? s in c.Visit(inside, row!.Value, row.Value + new Vector2(1, 1))) yield return s;
            c.LayWire(c.Row(0, 0, 2));
            c.Place(BC(Ids.ComputerCore), c.T(0, 0));
            c.Place(BC(Ids.PowerRack), c.T(1, 0));
            for (int i = 0; i < 4; i++)
            {
                c.StandBeside(c.T(1, 0));
                c.Hold(Battery);
                c.Action(c.T(1, 0));
            }
            c.S.Networks.Invalidate(inside);
            var n = c.NetworkAt(c.T(0, 0));
            c.Check($"{inside.NameOrUniqueName}: Core network is powered", n != null && c.S.Power.IsPowered(c.Nets, n), n == null ? "no network" : c.S.Power.Describe(c.Nets, n));
            c.StandAt(c.T(1, 1), Game1.up);
            yield return c.Show($"A Core and Power Rack inside the {(b is Building { buildingType.Value: "Shed" } ? "Shed" : inside.DisplayName)}", 70);
        }
        foreach (object? s in c.Visit(farm, Vector2.Zero, ports[0] + new Vector2(0, 1))) yield return s;
        var outside = c.S.Networks.For(farm).NetworkAt(ports[0]);
        var group = outside == null ? null : c.S.Networks.GroupOf(outside);
        c.Check("the Ports join the farm into one group", group is { Members.Count: >= 4, Working: true }, $"group members: {group?.Members.Count ?? 0}");
        foreach (object? s in c.GoHome()) yield return s;

        c.Remove(PrismaticShard, c.Carried(PrismaticShard));
        Game1.netWorldState.Value.GoldenWalnutsFound = 99;
        foreach (object? s in ChapterDone(c, 5)) yield return s;
        c.Check($"{TierId.T6WholeFarm} letter waits with no Prismatic Shard and 99 Golden Walnuts", !Quest.LetterCame(c, TierId.T6WholeFarm), Gates(c, TierId.T6WholeFarm));
        Game1.netWorldState.Value.GoldenWalnutsFound = 100;
        NoLetterYet(c, TierId.T6WholeFarm, "there's no Prismatic Shard in the backpack");
        c.Give(PrismaticShard);
        foreach (object? s in LetterArrives(c, TierId.T6WholeFarm, "Linked buildings, Ginger Island, Qi's Walnut Room and a Prismatic Shard: the last blueprints")) yield return s;
    }

    // ---- Chapter 6: the whole farm ----

    private static IEnumerable<object?> WholeFarm(SelfTestContext c)
    {
        c.CraftTimes(Ids.AdvancedCraftingRack, 1);
        c.Place(BC(Ids.AdvancedCraftingRack), c.T(7, 0));
        c.S.Networks.Invalidate(c.Loc);
        var net = c.NetworkAt(c.T(0, 0))!;
        c.Check("the Advanced Crafting Rack is on the network", net.OfKind(Core.Graph.BlockKind.AdvCraftingRack).Any());

        // Keep 60 Copper Wire in stock: the overnight run starts a job for what's short.
        string? error = c.S.Crafting.SetStockRule(c.Nets, net, new ItemKey(O(Ids.CopperWire), 0), 60);
        c.Check("keep-in-stock rule for 60 Copper Wire is set", error == null, error ?? "");
        c.Give(CopperBar, 5);
        c.Give(Fiber, 5);
        var menu = c.OpenTerminal(c.T(5, 0));
        foreach (string id in new[] { CopperBar, Fiber })
            if (menu != null && c.SlotOf(id) is >= 0 and var slot)
                c.ClickInventorySlot(menu, slot);
        c.CloseMenu();
        Quest.Evening(c);
        net = c.NetworkAt(c.T(0, 0))!;
        var core = c.Loc.objects[c.T(0, 0)];
        c.Check("overnight, keep-in-stock starts a job", CraftingService.JobsOf(core).Jobs.Count > 0);
        for (int i = 0; i < 12 && SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), O(Ids.CopperWire)) < 60; i++)
            foreach (object? s in c.TimePasses()) yield return s;
        c.Check("Copper Wire topped up to 60", SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), O(Ids.CopperWire)) >= 60,
            $"stored {SelfTestContext.Stored(c.StorageAt(c.T(5, 0)), O(Ids.CopperWire))}");
        c.StandAt(c.T(7, 1), Game1.up);
        yield return c.Show("The Advanced Crafting Rack keeps 60 Copper Wire in stock");

        c.CraftTimes(Ids.PrismaticDisk, 1);
        c.CheckEqual("the Prismatic Disk used the Iridium Disk", 0, c.Carried(O(Ids.IridiumDisk)));

        // Island Uplinks: one in the base, one on Ginger Island.
        c.CraftTimes(Ids.IslandUplink, 2);
        c.Place(BC(Ids.IslandUplink), c.T(8, 0));
        var island = Game1.getLocationFromName("IslandWest");
        c.Require("Ginger Island (IslandWest) exists", island is IslandLocation);
        var row = c.FindFreeRow(island!, 2);
        c.Require("free ground on Ginger Island for the Uplink", row != null);
        foreach (object? s in c.Visit(island!, row!.Value, row.Value + new Vector2(0, 1))) yield return s;
        c.LayWire(c.Row(0, 0, 1));
        c.Place(BC(Ids.IslandUplink), c.T(0, 0));
        c.StandAt(c.T(0, 1), Game1.up);
        yield return c.Show("An Island Uplink on Ginger Island", 80);
        foreach (object? s in c.GoHome()) yield return s;
        net = c.NetworkAt(c.T(0, 0))!;
        var group = c.S.Networks.GroupOf(net);
        c.Check("the Uplinks join the island to the base", group is { Members.Count: >= 2 } && c.S.Networks.Members(c.Nets, net).Any(m => m.Loc.Location is IslandLocation),
            $"group members: {group?.Members.Count ?? 0}");

        foreach (object? s in c.TimePasses()) yield return s; // the finale ticks once everything else has
        foreach (object? s in ChapterDone(c, 6)) yield return s;
    }

    // ---- The end ----

    private static IEnumerable<object?> Finale(SelfTestContext c)
    {
        Quest.Morning(c, PowerPass);
        var done = Quest.Done(c);
        var all = GuideBook.AllQuests.Select(q => q.Id).ToList();
        c.CheckEqual("PROGRESSION: every journal goal ticked", all.Count, all.Count(done.Contains));
        foreach (var tier in ContentInjector.TiersWithMail)
            c.Check($"PROGRESSION: Maru's {tier} letter read", c.Player.mailReceived.Contains(ContentInjector.MailId(tier)));
        var unknown = ContentInjector.AllRecipes.Where(r => !c.Player.craftingRecipes.ContainsKey(r)).ToList();
        c.Check("PROGRESSION: every recipe learned", unknown.Count == 0, string.Join(", ", unknown));
        var neverMade = ContentInjector.AllRecipes.Where(r => c.Player.craftingRecipes.TryGetValue(r, out int n) && n == 0).ToList();
        c.Check("every recipe crafted at least once", neverMade.Count == 0, "never crafted: " + string.Join(", ", neverMade.Select(SelfTestContext.Short)));
        var built = Progression.ProgressionService.TiersBuilt();
        c.Check("PROGRESSION: all six tiers built", ContentInjector.TiersWithMail.All(built.Contains), string.Join(",", built));

        c.S.Guide.Open();
        yield return c.Show("The journal's last page: every goal ticked", 150);
        c.CloseMenu();
        c.StandAt(c.T(4, 5), Game1.up);
        c.HoldNothing();
        yield return c.Show("The finished base, left standing for you to try", 120);
    }
}
