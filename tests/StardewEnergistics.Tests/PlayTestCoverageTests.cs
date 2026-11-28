using System.Text.RegularExpressions;
using StardewEnergistics.Core.Guide;
using StardewEnergistics.Core.Testing;

namespace StardewEnergistics.Tests;

/// <summary>
/// Every feature ships with a simulated play test: an item added to Ids.cs, a journal goal or a tier letter with
/// no entry in <see cref="PlayTestCoverage"/> fails here. Add the scenario (docs/testing/selftest.md) and the entry.
/// </summary>
public class PlayTestCoverageTests
{
    private static IEnumerable<string> ItemKeysInIds()
    {
        string ids = File.ReadAllText(Path.Combine(RepoPaths.Root, "src", "StardewEnergistics", "Ids.cs"));
        return Regex.Matches(ids, @"public const string \w+ = Prefix \+ ""(\w+)"";").Select(m => m.Groups[1].Value);
    }

    [Fact]
    public void EveryItemHasAPlayTest()
    {
        var items = ItemKeysInIds().ToList();
        Assert.True(items.Count >= 25, $"only found {items.Count} items in Ids.cs; has its layout changed?");
        var missing = PlayTestCoverage.Missing(items.Select(k => "item:" + k)).ToList();
        Assert.True(missing.Count == 0, "Items with no play-test scenario (add one and list it in PlayTestCoverage): " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryJournalGoalAndLetterHasAPlayTest()
    {
        var required = GuideBook.AllQuests.Select(q => "quest:" + q.Id).Concat(Enumerable.Range(1, 6).Select(n => "letter:T" + n));
        var missing = PlayTestCoverage.Missing(required).ToList();
        Assert.True(missing.Count == 0, "Journal goals or letters with no play test: " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryScenarioNamedExists()
    {
        var unknown = PlayTestCoverage.UnknownScenarios().ToList();
        Assert.True(unknown.Count == 0, "Coverage names scenarios that don't exist: " + string.Join(", ", unknown));
    }

    [Fact]
    public void EveryScenarioIsRegisteredInTheSelfTest()
    {
        string scenarios = File.ReadAllText(Path.Combine(RepoPaths.Root, "src", "StardewEnergistics", "Testing", "SelfTest", "Scenarios.cs"));
        var registered = Regex.Matches(scenarios, @"new\(""([\w-]+)"",").Select(m => m.Groups[1].Value).ToHashSet();
        var notRegistered = PlayTestCoverage.ScenarioIds.Where(s => !registered.Contains(s)).ToList();
        var notListed = registered.Where(s => !PlayTestCoverage.ScenarioIds.Contains(s)).ToList();
        Assert.True(notRegistered.Count == 0 && notListed.Count == 0,
            $"Scenarios.All and PlayTestCoverage.ScenarioIds differ. Not registered: {string.Join(", ", notRegistered)}; not listed: {string.Join(", ", notListed)}");
    }
}
