using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Guide;
using StardewEnergistics.Core.Progression;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// The quest side, through the real paths: the journal's morning check (Maru's journal letter), reading mail at
/// the mailbox (the game's own letter screen learns the recipes and hands over attachments), Maru's tier
/// letters as her hearts and the player's progress grow, and the journal's goals ticking off.
/// Changes the player's mail, recipes, Maru friendship and stats, so it only runs in launched sessions.
/// </summary>
internal static class Quest
{
    /// <summary>Starts the player over as a new farmer as far as this mod is concerned.</summary>
    public static void FreshStart(SelfTestContext c)
    {
        var p = c.Player;
        c.Step("start the quest over: no mod mail, recipes, journal ticks or Maru hearts");
        foreach (string id in p.mailReceived.Where(m => m.StartsWith(Ids.ModId)).ToList())
            p.mailReceived.Remove(id);
        foreach (string id in p.mailbox.Where(m => m.StartsWith(Ids.ModId)).ToList())
            p.mailbox.Remove(id);
        foreach (string id in p.mailForTomorrow.Where(m => m.StartsWith(Ids.ModId)).ToList())
            p.mailForTomorrow.Remove(id);
        foreach (string id in ContentInjector.AllRecipes.Append(Ids.QuestJournal))
            p.craftingRecipes.Remove(id);
        p.modData.Remove(Ids.Data.GuideDone);
        var farm = Game1.getFarm();
        farm.modData.Remove(Ids.Data.GuideWorld);
        farm.modData.Remove(Ids.Data.TiersBuilt);
        SetMaruHearts(c, 0);
        p.stats.Set(Ids.StatCopperProcessors, 0);
        p.deepestMineLevel = 0;
        c.S.Progression.GatesEnabled = true;
    }

    public static void SetMaruHearts(SelfTestContext c, int hearts)
    {
        if (!c.Player.friendshipData.TryGetValue("Maru", out var f))
            c.Player.friendshipData["Maru"] = f = new Friendship();
        f.Points = hearts * 250;
        if (hearts == 0)
        {
            // A farmer who has never spoken to Maru.
            f.TalkedToToday = false;
            f.GiftsToday = 0;
        }
    }

    /// <summary>Talks to Maru once, as the game does: 20 friendship points and "talked today".</summary>
    public static void TalkToMaru(SelfTestContext c)
    {
        c.Step("talk to Maru");
        if (!c.Player.friendshipData.TryGetValue("Maru", out var f))
            c.Player.friendshipData["Maru"] = f = new Friendship();
        if (!f.TalkedToToday)
            f.Points += 20;
        f.TalkedToToday = true;
    }

    /// <summary>
    /// A new morning as far as this mod is concerned (a real new day would save): yesterday's letters arrive,
    /// then the mod's DayStarted handlers run in their real order: autocraft summary, journal, optionally the
    /// day's power use (<paramref name="powerPass"/>; it touches every network), then Maru's tier letters.
    /// </summary>
    public static void Morning(SelfTestContext c, bool powerPass = false)
    {
        c.Step("morning: yesterday's letters, journal check" + (powerPass ? ", the day's power" : "") + " and Maru's letters");
        foreach (string id in c.Player.mailForTomorrow.Where(m => m.StartsWith(Ids.ModId)).ToList())
        {
            c.Player.mailForTomorrow.Remove(id);
            if (!c.Player.mailbox.Contains(id))
                c.Player.mailbox.Add(id);
        }
        c.S.Crafting.OnDayStarted();
        Call(c, c.S.Guide, "OnDayStarted");
        if (powerPass)
            c.S.Power.OnDayStarted();
        c.S.Progression.OnDayStarted();
        c.S.Networks.Clear();
    }

    /// <summary>The mod's evening work (DayEnding: Links finish up, keep-in-stock jobs, Shipping Links), without the save.</summary>
    public static void Evening(SelfTestContext c)
    {
        c.Step("evening: Links finish up, keep-in-stock jobs start, Shipping Links ship");
        c.S.Links.OnDayEnding();
        c.S.Crafting.OnDayEnding();
        c.S.Shipping.OnDayEnding();
        c.S.Networks.Clear();
    }

    /// <summary>True if Maru's letter for this tier has arrived or been read.</summary>
    public static bool LetterCame(SelfTestContext c, TierId tier) => c.Player.hasOrWillReceiveMail(ContentInjector.MailId(tier));

    /// <summary>The journal re-checks goals (it does this every 10 minutes and when menus close).</summary>
    public static void Refresh(SelfTestContext c) => Call(c, c.S.Guide, "Refresh");

    private static void Call(SelfTestContext c, object target, string method) =>
        c.Helper.Reflection.GetMethod(target, method).Invoke();

    public static HashSet<string> Done(SelfTestContext c) =>
        GuideProgress.Decode(c.Player.modData.TryGetValue(Ids.Data.GuideDone, out string? raw) ? raw : null);

    /// <summary>
    /// Opens the top letter in the mailbox the way the farm mailbox does, takes any attachment, and returns the
    /// letter screen (still open, for a screenshot). Null if there's no letter or it didn't open.
    /// </summary>
    public static LetterViewerMenu? ReadMail(SelfTestContext c, string mailId)
    {
        c.Step($"read letter {mailId.Replace(Ids.ModId + "_", "")} at the mailbox");
        c.Require($"{Short(mailId)} letter is in the mailbox", c.Player.mailbox.Contains(mailId), string.Join(", ", c.Player.mailbox));
        // The mailbox opens the first letter; put this one first.
        c.Player.mailbox.Remove(mailId);
        c.Player.mailbox.Insert(0, mailId);
        c.CloseMenu();
        Game1.getFarm().mailbox();
        var letter = Game1.activeClickableMenu as LetterViewerMenu;
        c.Check($"{Short(mailId)} letter opens", letter != null, Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        c.Check($"{Short(mailId)} letter is marked read", c.Player.mailReceived.Contains(mailId));
        if (letter != null)
        {
            foreach (var slot in letter.itemsToGrab.Where(s => s.item != null))
            {
                c.Log($"took attachment {slot.item.QualifiedItemId} x{slot.item.Stack}");
                c.Player.addItemToInventoryBool(slot.item);
                slot.item = null;
            }
        }
        return letter;
    }

    private static string Short(string mailId) => mailId.Replace(Ids.ModId + "_Maru_", "");

    /// <summary>Checks the player now knows every recipe Maru's letter for this tier teaches.</summary>
    public static void CheckRecipes(SelfTestContext c, TierId tier)
    {
        var missing = ContentInjector.RecipesFor(tier).Where(r => !c.Player.craftingRecipes.ContainsKey(r)).ToList();
        c.Check($"{tier} recipes learned from the letter", missing.Count == 0, "missing: " + string.Join(", ", missing.Select(m => m.Replace(Ids.Prefix, ""))));
    }

    /// <summary>Day one to Maru's second letter, with screenshots when run as the showcase.</summary>
    public static IEnumerable<object?> Prologue(SelfTestContext c)
    {
        FreshStart(c);
        var p = c.Player;
        c.Check("fresh start: no mod recipes known", !ContentInjector.AllRecipes.Any(p.craftingRecipes.ContainsKey));

        // Day one: Maru mails the journal.
        Morning(c);
        c.Check("day one: journal recipe known", p.craftingRecipes.ContainsKey(Ids.QuestJournal));
        c.Check("day one: no blueprints yet (not talked to Maru)", !p.mailbox.Contains(ContentInjector.MailId(TierId.T1Components)));
        var letter = ReadMail(c, ContentInjector.GuideMailId);
        c.CheckEqual("the journal came with the letter", 1, c.Carried("(O)" + Ids.QuestJournal));
        yield return c.Show("Day one: Maru's letter with the Tinkerer's Journal", 110, 40);
        c.CloseMenu();

        // Open the journal by using it.
        int slot = c.SlotOf("(O)" + Ids.QuestJournal);
        if (slot >= 0)
        {
            c.Player.CurrentToolIndex = slot;
            c.Step("use the journal");
            c.Input.Press(SButton.MouseRight, c.Player.GetGrabTile());
        }
        c.Check("using the journal opens it", Game1.activeClickableMenu?.GetType().Name == "GuideMenu", Game1.activeClickableMenu?.GetType().Name ?? "no menu");
        c.Check("journal goal 'open' ticked", Done(c).Contains("open"));
        yield return c.Show("The Tinkerer's Journal, open on the first goal", 110);
        c.CloseMenu();

        // Talk to Maru once: the first blueprints next morning.
        TalkToMaru(c);
        Morning(c);
        letter = ReadMail(c, ContentInjector.MailId(TierId.T1Components));
        CheckRecipes(c, TierId.T1Components);
        yield return c.Show("One chat with Maru: her first blueprints (Processor Press, processors, wire)", 110, 40);
        c.CloseMenu();
        Refresh(c);
        c.Check("journal goals 'maru' and 'letter1' ticked", Done(c).Contains("maru") && Done(c).Contains("letter1"), string.Join(",", Done(c)));

        // Ten Copper Processors pressed, Mines floor 20 and 2 hearts: storage blueprints.
        p.stats.Set(Ids.StatCopperProcessors, 10);
        SetMaruHearts(c, 2);
        Morning(c);
        c.Check("T2 waits for Mines floor 20", !p.mailbox.Contains(ContentInjector.MailId(TierId.T2SimpleStorage)));
        p.deepestMineLevel = 20;
        Morning(c);
        letter = ReadMail(c, ContentInjector.MailId(TierId.T2SimpleStorage));
        CheckRecipes(c, TierId.T2SimpleStorage);
        yield return c.Show("10 processors, Mines floor 20, 2 hearts: storage blueprints (Core, racks, Terminal, disks)", 110, 40);
        c.CloseMenu();
        Refresh(c);
        c.Check("journal goals 'tenChips' and 'letter2' ticked", Done(c).Contains("tenChips") && Done(c).Contains("letter2"), string.Join(",", Done(c)));
    }

    /// <summary>After a base is built: the journal ticks network goals, and Maru's next letters follow.</summary>
    public static IEnumerable<object?> Epilogue(SelfTestContext c, string[] expectTicked, bool laterLetters = true)
    {
        Refresh(c);
        var done = Done(c);
        foreach (string id in expectTicked)
            c.Check($"journal goal '{id}' ticked", done.Contains(id));

        c.Step("open the journal on the network chapter");
        c.S.Guide.Open();
        yield return c.Show($"The journal after building: {done.Count} of {GuideBook.AllQuests.Count()} goals ticked", 120);
        c.CloseMenu();
        if (!laterLetters)
            yield break;

        // The base counts as built tiers; with more hearts and depth, Maru's later letters arrive.
        SetMaruHearts(c, 7);
        c.Player.deepestMineLevel = 120;
        Morning(c);
        Morning(c); // T4 needs T3 marked built first, which the first morning records
        foreach (var tier in new[] { TierId.T3Automation, TierId.T4Autocrafting })
        {
            if (!c.Player.mailbox.Contains(ContentInjector.MailId(tier)))
            {
                c.Check($"{tier} letter arrives", false, "tiers built: " + string.Join(",", StardewEnergistics.Progression.ProgressionService.TiersBuilt().Select(t => t.ToString())));
                continue;
            }
            ReadMail(c, ContentInjector.MailId(tier));
            CheckRecipes(c, tier);
            yield return c.Show($"Maru's {(tier == TierId.T3Automation ? "automation" : "autocrafting")} blueprints arrive once the base is built", 100, 40);
            c.CloseMenu();
        }
    }

    /// <summary>The quest scenario for a plain launched run (no slow pauses needed, but screenshots are cheap).</summary>
    public static IEnumerable<object?> Run(SelfTestContext c)
    {
        foreach (object? step in Prologue(c))
            yield return step;

        c.BuildBasicNetwork();
        c.InsertDisk(c.T(1, 0), c.NewDisk());
        foreach (object? step in Epilogue(c, new[] { "core", "disk", "power" }, laterLetters: false))
            yield return step;
    }
}
