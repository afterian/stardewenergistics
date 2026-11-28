using StardewEnergistics.Core.Progression;

namespace StardewEnergistics.Tests;

public class GateEvaluatorTests
{
    private static ProgressSnapshot Snap(int hearts = 0, int mine = 0, int skull = 0, int processors = 0, bool iridium = false,
        bool island = false, bool walnut = false, bool prismatic = false, params TierId[] built) =>
        new(hearts, mine, skull, processors, iridium, island, walnut, prismatic, new HashSet<TierId>(built));

    [Fact]
    public void NewFarmHasNothing() => Assert.Empty(new GateEvaluator().Unlocked(Snap()));

    [Fact]
    public void MaruTwoHeartsUnlocksComponents() =>
        Assert.Equal(new[] { TierId.T1Components }, new GateEvaluator().Unlocked(Snap(hearts: 2)));

    [Fact]
    public void T2NeedsTenProcessorsAndMineTwenty()
    {
        var g = new GateEvaluator();
        Assert.DoesNotContain(TierId.T2SimpleStorage, g.Unlocked(Snap(hearts: 2, mine: 20, processors: 9)));
        Assert.DoesNotContain(TierId.T2SimpleStorage, g.Unlocked(Snap(hearts: 2, mine: 19, processors: 10)));
        Assert.Contains(TierId.T2SimpleStorage, g.Unlocked(Snap(hearts: 2, mine: 20, processors: 10)));
    }

    [Fact]
    public void T3NeedsT2Built()
    {
        var g = new GateEvaluator();
        Assert.DoesNotContain(TierId.T3Automation, g.Unlocked(Snap(hearts: 4, mine: 80, processors: 10)));
        Assert.Contains(TierId.T3Automation, g.Unlocked(Snap(hearts: 4, mine: 80, processors: 10, built: TierId.T2SimpleStorage)));
    }

    [Fact]
    public void FullProgressUnlocksAllSix()
    {
        var s = Snap(10, 120, 100, 50, true, true, true, true,
            TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation, TierId.T4Autocrafting, TierId.T5LinkedBuildings);
        Assert.Equal(6, new GateEvaluator().Unlocked(s).Count);
    }

    [Fact]
    public void SpendingTheIridiumBarAfterBuildingT5DoesNotLockT6()
    {
        // Found by the play-tester: the Iridium Bar went into an Iridium Disk, and Maru's T6 letter never came.
        var s = Snap(8, 120, 25, 10, iridium: false, island: true, walnut: true, prismatic: true,
            built: new[] { TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation, TierId.T4Autocrafting, TierId.T5LinkedBuildings });
        Assert.Contains(TierId.T6WholeFarm, new GateEvaluator().Unlocked(s));
        Assert.True(GateRequirements.AllMet(TierId.T6WholeFarm, s));
    }

    [Fact]
    public void LosingHeartsAfterBuildingKeepsLaterTiersOpen()
    {
        var s = Snap(hearts: 1, mine: 120, processors: 10, built: new[] { TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation });
        Assert.Contains(TierId.T4Autocrafting, new GateEvaluator().Unlocked(s with { MaruHearts = 6 }));
        Assert.Contains(TierId.T3Automation, new GateEvaluator().Unlocked(s));
    }

    [Fact]
    public void GatesOffUnlocksEverything() => Assert.Equal(6, new GateEvaluator(gatesEnabled: false).Unlocked(Snap()).Count);
}
