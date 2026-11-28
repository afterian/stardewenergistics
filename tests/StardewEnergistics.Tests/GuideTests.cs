using System.Text.Json;
using System.Text.RegularExpressions;
using StardewEnergistics.Core.Guide;
using StardewEnergistics.Core.Progression;

namespace StardewEnergistics.Tests;

public class GuideTests
{
    private static readonly Lazy<Dictionary<string, string>> I18n = new(() =>
    {
        string path = Path.Combine(RepoPaths.Root, "src", "StardewEnergistics", "i18n", "default.json");
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        using var doc = JsonDocument.Parse(File.ReadAllText(path), options);
        return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? "");
    });

    private static ProgressSnapshot Snap(int hearts = 0, int mine = 0, int skull = 0, int processors = 0, bool iridium = false,
        bool island = false, bool walnut = false, bool prismatic = false, params TierId[] built) =>
        new(hearts, mine, skull, processors, iridium, island, walnut, prismatic, new HashSet<TierId>(built), TalkedToMaru: hearts > 0);

    private static GuideFacts Facts(ProgressSnapshot? s = null, bool opened = false, IEnumerable<TierId>? letters = null,
        IEnumerable<string>? got = null, IEnumerable<WorldFlag>? world = null) =>
        new(s ?? Snap(), opened, (letters ?? Array.Empty<TierId>()).ToHashSet(), (got ?? Array.Empty<string>()).ToHashSet(),
            (world ?? Array.Empty<WorldFlag>()).ToHashSet());

    private static GuideFacts Everything() => Facts(
        Snap(10, 120, 100, 50, true, true, true, true),
        opened: true,
        letters: Enum.GetValues<TierId>(),
        got: GuideBook.AllQuests.Select(q => q.Goal.Item).OfType<string>(),
        world: Enum.GetValues<WorldFlag>());

    [Fact]
    public void QuestIdsAreUnique()
    {
        var ids = GuideBook.AllQuests.Select(q => q.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void ChaptersCoverEveryTierInOrder()
    {
        Assert.Equal(Enumerable.Range(0, GuideBook.Chapters.Count), GuideBook.Chapters.Select(c => c.Number));
        Assert.Equal(Enum.GetValues<TierId>().Cast<TierId?>(), GuideBook.Chapters.Skip(1).Select(c => c.Tier));
        Assert.All(GuideBook.Chapters, c => Assert.All(c.Quests, q => Assert.Equal(c.Number, q.Chapter)));
    }

    [Fact]
    public void EachTierChapterStartsWithItsLetter()
    {
        foreach (var c in GuideBook.Chapters.Where(c => c.Tier != null))
            Assert.Equal(GuideGoal.Letter(c.Tier!.Value), c.Quests[0].Goal);
    }

    [Fact]
    public void EveryModItemIsInTheJournal()
    {
        var items = I18n.Value.Keys
            .Select(k => Regex.Match(k, @"^item\.(\w+)\.name$"))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .ToList();
        Assert.True(items.Count >= 27, "expected every mod item in i18n");
        var covered = GuideBook.AllQuests.SelectMany(q => q.Items).ToHashSet();
        Assert.DoesNotContain(items, i => !covered.Contains(i));
        Assert.DoesNotContain(covered, i => !items.Contains(i));
    }

    [Fact]
    public void EveryQuestAndChapterHasText()
    {
        var keys = GuideBook.AllQuests.SelectMany(q => new[] { $"guide.q.{q.Id}.title", $"guide.q.{q.Id}.text" })
            .Concat(GuideBook.Chapters.SelectMany(c => new[] { $"guide.ch.{c.Number}.title", $"guide.ch.{c.Number}.intro" }))
            .Concat(GuideBook.Chapters.Where(c => c.RewardItem != null).SelectMany(c => new[] { $"mail.guideCh{c.Number}.title", $"mail.guideCh{c.Number}.text" }));
        Assert.DoesNotContain(keys, k => !I18n.Value.TryGetValue(k, out string? v) || string.IsNullOrWhiteSpace(v));
    }

    [Fact]
    public void EveryGateRequirementHasText()
    {
        var s = Snap();
        var keys = Enum.GetValues<TierId>().SelectMany(t => GateRequirements.For(t, s)).Select(r => $"guide.req.{r.Key}").Distinct();
        Assert.DoesNotContain(keys, k => !I18n.Value.ContainsKey(k));
    }

    [Fact]
    public void PressedProcessorsHavePressText()
    {
        foreach (string p in new[] { "CopperProcessor", "GoldProcessor", "IridiumProcessor" })
            Assert.True(I18n.Value.ContainsKey($"guide.press.{p}"), p);
    }

    [Fact]
    public void JournalTextUsesOnlyFontSafeCharacters()
    {
        // Stardew's sprite fonts lack curly quotes, dashes and most symbols.
        foreach (var (key, value) in I18n.Value.Where(kv => kv.Key.StartsWith("guide.") || kv.Key.StartsWith("mail.guide")))
            Assert.True(value.All(ch => ch == '\n' || (ch >= ' ' && ch <= '~')), key);
    }

    [Fact]
    public void NewPlayerHasNothingDone() => Assert.Empty(GuideProgress.Update(Facts(), Array.Empty<string>()));

    [Fact]
    public void OpeningTheJournalTicksTheFirstGoal()
    {
        var done = GuideProgress.Update(Facts(opened: true), Array.Empty<string>());
        Assert.Equal(new[] { "open" }, done);
        Assert.Equal("maru", GuideProgress.Next(done)!.Id);
    }

    [Fact]
    public void GoalsStayDoneWhenTheFactGoesAway()
    {
        var first = GuideProgress.Update(Facts(got: new[] { "GoldProcessor" }, world: new[] { WorldFlag.NetworkPowered }), Array.Empty<string>());
        Assert.Contains("goldChip", first);
        Assert.Contains("power", first);
        var later = GuideProgress.Update(Facts(), first);
        Assert.Equal(first, later);
    }

    [Fact]
    public void UnknownSavedIdsAreDropped() =>
        Assert.Empty(GuideProgress.Update(Facts(), new[] { "removedQuest" }));

    [Fact]
    public void ProcessorGoalsCount()
    {
        var f = Facts(Snap(processors: 4));
        var chip = GuideBook.Find("chip")!;
        var ten = GuideBook.Find("tenChips")!;
        Assert.True(GuideProgress.IsMet(chip.Goal, f));
        Assert.False(GuideProgress.IsMet(ten.Goal, f));
        Assert.Equal((4, 5), GuideProgress.Counter(ten.Goal, f));
        Assert.Equal((5, 5), GuideProgress.Counter(ten.Goal, Facts(Snap(processors: 25))));
        Assert.Null(GuideProgress.Counter(GuideBook.Find("press")!.Goal, f));
    }

    [Fact]
    public void FinaleNeedsEverythingElse()
    {
        var all = GuideProgress.Update(Everything(), Array.Empty<string>());
        Assert.Contains("finale", all);
        Assert.Null(GuideProgress.Next(all));
        Assert.All(GuideBook.Chapters, c => Assert.True(GuideProgress.ChapterDone(c, all)));

        var almost = GuideProgress.Update(Everything() with { World = new HashSet<WorldFlag>() }, Array.Empty<string>());
        Assert.DoesNotContain("finale", almost);
    }

    [Fact]
    public void FinishingAChapterIsReportedOnce()
    {
        var ch1 = GuideBook.Chapters[1];
        var before = new HashSet<string>(ch1.Quests.Skip(1).Select(q => q.Id));
        var now = new HashSet<string>(before) { ch1.Quests[0].Id };
        Assert.Equal(new[] { ch1 }, GuideProgress.NewlyFinishedChapters(before, now));
        Assert.Empty(GuideProgress.NewlyFinishedChapters(now, now));
    }

    [Fact]
    public void SavedFormRoundTrips()
    {
        var done = new HashSet<string> { "wire", "open", "core" };
        Assert.Equal("core,open,wire", GuideProgress.Encode(done));
        Assert.Equal(done, GuideProgress.Decode(GuideProgress.Encode(done)));
        Assert.Empty(GuideProgress.Decode(null));
        Assert.Empty(GuideProgress.Decode(""));
    }

    [Fact]
    public void GateRequirementsShowProgress()
    {
        var reqs = GateRequirements.For(TierId.T3Automation, Snap(hearts: 3, mine: 64));
        Assert.Equal(new[] { "built2", "mines", "maru" }, reqs.Select(r => r.Key));
        Assert.Equal((64, 80), (reqs[1].Have, reqs[1].Need));
        Assert.False(reqs[2].Met);
    }

    [Fact]
    public void TheFirstLetterOnlyNeedsOneChatWithMaru()
    {
        Assert.Equal(new[] { "talkedMaru" }, GateRequirements.For(TierId.T1Components, Snap()).Select(r => r.Key));
        Assert.Contains("maru", GateRequirements.For(TierId.T2SimpleStorage, Snap()).Select(r => r.Key));
        var done = GuideProgress.Update(Facts(Snap() with { TalkedToMaru = true }, opened: true), Array.Empty<string>());
        Assert.Contains("maru", done);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GateRequirementsAgreeWithTheGateEvaluator(int seed)
    {
        var rng = new Random(seed);
        var g = new GateEvaluator();
        int[] hearts = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 10 }, mines = { 0, 19, 20, 79, 80, 119, 120 }, skull = { 0, 24, 25, 60 }, procs = { 0, 4, 5, 30 };
        for (int i = 0; i < 2000; i++)
        {
            var built = Enum.GetValues<TierId>().Where(_ => rng.Next(3) > 0).ToArray();
            var s = Snap(hearts[rng.Next(hearts.Length)], mines[rng.Next(mines.Length)], skull[rng.Next(skull.Length)], procs[rng.Next(procs.Length)],
                rng.Next(2) == 0, rng.Next(2) == 0, rng.Next(2) == 0, rng.Next(2) == 0, built);
            var unlocked = g.Unlocked(s);
            foreach (var t in Enum.GetValues<TierId>())
                Assert.Equal(unlocked.Contains(t), GateRequirements.AllMet(t, s));
        }
    }
}
