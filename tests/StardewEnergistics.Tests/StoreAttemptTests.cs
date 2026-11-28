using StardewEnergistics.Core.Storage;
using Xunit;

namespace StardewEnergistics.Tests;

public class StoreAttemptTests
{
    private sealed class Stack
    {
        public int Count;
    }

    [Fact]
    public void EverythingStoredLeavesNothingToGiveBack()
    {
        var item = new Stack { Count = 10 };
        Assert.Null(StoreAttempt.Leftover(item, _ => null));
    }

    [Fact]
    public void NoStorageGivesTheWholeItemBack()
    {
        var item = new Stack { Count = 10 };
        Assert.Same(item, StoreAttempt.Leftover<Stack>(item, null));
    }

    [Fact]
    public void PartlyStoredGivesBackOnlyTheRest()
    {
        var item = new Stack { Count = 10 };
        var left = StoreAttempt.Leftover(item, s => { s.Count = 3; return s; });
        Assert.Equal(3, left!.Count);
    }
}
