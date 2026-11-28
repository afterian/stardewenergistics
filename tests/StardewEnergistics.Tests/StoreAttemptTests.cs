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

public class StoreRefusalTests
{
    [Fact]
    public void AKindAlreadyOnADiskIsRefusedForItemsNotKinds()
        => Assert.Equal(StoreRefusal.Full, StoreRefusals.Why(true, 1, 20, 20, kindAlreadyStored: true));

    [Fact]
    public void ANewKindWithEveryKindUsedIsKindsFull()
        => Assert.Equal(StoreRefusal.KindsFull, StoreRefusals.Why(true, 1, 20, 20, kindAlreadyStored: false));

    [Fact]
    public void NoDisksAndUnstorableItemsSaySo()
    {
        Assert.Equal(StoreRefusal.NoDisks, StoreRefusals.Why(true, 0, 0, 0, false));
        Assert.Equal(StoreRefusal.CantStore, StoreRefusals.Why(false, 3, 0, 60, false));
    }

    [Fact]
    public void UnlimitedKindsIsNeverKindsFull()
        => Assert.Equal(StoreRefusal.Full, StoreRefusals.Why(true, 1, 500, 0, false));
}
