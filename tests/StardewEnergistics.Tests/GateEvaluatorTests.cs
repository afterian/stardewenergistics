using StardewEnergistics.Core.Progression;

namespace StardewEnergistics.Tests;

public class GateEvaluatorTests
{
    private static ProgressSnapshot Snap(int hearts = 0, int mine = 0, int skull = 0, int processors = 0, bool iridium = false,
        bool island = false, bool walnut = false, bool prismatic = false, params TierId[] built) =>
        new(hearts, mine, skull, processors, iridium, island, walnut, prismatic, new HashSet<TierId>(built), TalkedToMaru: hearts > 0);

    [Fact]
    public void NewFarmHasNothing() => Assert.Empty(new GateEvaluator().Unlocked(Snap()));

    [Fact]
    public void TalkingToMaruOnceUnlocksComponents() =>
        Assert.Equal(new[] { TierId.T1Components }, new GateEvaluator().Unlocked(Snap() with { TalkedToMaru = true }));

    [Fact]
    public void ComponentsWaitUntilYouTalkToMaru() =>
        Assert.Empty(new GateEvaluator().Unlocked(Snap(mine: 120, processors: 50) with { TalkedToMaru = false }));

    [Fact]
    public void T2NeedsFiveProcessorsMineTwentyAndTwoHearts()
    {
        var g = new GateEvaluator();
        Assert.DoesNotContain(TierId.T2SimpleStorage, g.Unlocked(Snap(hearts: 1, mine: 20, processors: 5)));
        Assert.DoesNotContain(TierId.T2SimpleStorage, g.Unlocked(Snap(hearts: 2, mine: 20, processors: 4)));
        Assert.DoesNotContain(TierId.T2SimpleStorage, g.Unlocked(Snap(hearts: 2, mine: 19, processors: 5)));
        Assert.Contains(TierId.T2SimpleStorage, g.Unlocked(Snap(hearts: 2, mine: 20, processors: 5)));
    }

    [Fact]
    public void T3NeedsT2Built()
    {
        var g = new GateEvaluator();
        Assert.DoesNotContain(TierId.T3Automation, g.Unlocked(Snap(hearts: 5, mine: 80, processors: 10)));
        Assert.DoesNotContain(TierId.T3Automation, g.Unlocked(Snap(hearts: 4, mine: 80, processors: 10, built: TierId.T2SimpleStorage)));
        Assert.Contains(TierId.T3Automation, g.Unlocked(Snap(hearts: 5, mine: 80, processors: 10, built: TierId.T2SimpleStorage)));
    }

    [Fact]
    public void FullProgressUnlocksAllSix()
    {
        var s = Snap(10, 120, 100, 50, true, true, true, true,
            TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation, TierId.T4Autocrafting, TierId.T5LinkedBuildings);
        Assert.Equal(6, new GateEvaluator().Unlocked(s).Count);
    }

    private static readonly TierId[] FirstFive = { TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation, TierId.T4Autocrafting, TierId.T5LinkedBuildings };

    [Fact]
    public void SpendingTheIridiumBarAfterTheT5LetterDoesNotLockT6()
    {
        // Found by the play-tester: the Iridium Bar went into an Iridium Disk, and Maru's T6 letter never came.
        var s = Snap(8, 120, 25, 10, iridium: false, island: true, walnut: true, prismatic: true, built: FirstFive) with { LettersSent = FirstFive.ToHashSet() };
        Assert.Contains(TierId.T6WholeFarm, new GateEvaluator().Unlocked(s));
        Assert.True(GateRequirements.AllMet(TierId.T6WholeFarm, s));
        Assert.DoesNotContain(TierId.T6WholeFarm, new GateEvaluator().Unlocked(s with { LettersSent = null }));
    }

    [Fact]
    public void LosingHeartsAfterALetterKeepsItsTierOpen()
    {
        var s = Snap(hearts: 1, mine: 120, processors: 10, built: new[] { TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation })
            with { LettersSent = new HashSet<TierId> { TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation } };
        Assert.Contains(TierId.T3Automation, new GateEvaluator().Unlocked(s));
        Assert.DoesNotContain(TierId.T4Autocrafting, new GateEvaluator().Unlocked(s));
        Assert.DoesNotContain(TierId.T4Autocrafting, new GateEvaluator().Unlocked(s with { MaruHearts = 6 }));
        Assert.Contains(TierId.T4Autocrafting, new GateEvaluator().Unlocked(s with { MaruHearts = 7 }));
    }

    [Fact]
    public void ABuiltNetworkDoesNotSkipALettersOwnRequirements()
    {
        // A save whose networks already count as T2 built still waits for 10 processors and Mines 20.
        var s = Snap(hearts: 2, mine: 20, processors: 1, built: TierId.T2SimpleStorage);
        Assert.DoesNotContain(TierId.T2SimpleStorage, new GateEvaluator().Unlocked(s));
    }

    [Fact]
    public void GatesOffUnlocksEverything() => Assert.Equal(6, new GateEvaluator(gatesEnabled: false).Unlocked(Snap()).Count);
}
