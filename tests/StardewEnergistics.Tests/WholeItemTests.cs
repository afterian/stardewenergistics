using StardewEnergistics.Core.Storage;
using Xunit;

namespace StardewEnergistics.Tests;

public class WholeItemTests
{
    private static DiskContents Copper() => new(new DiskId("d"), DiskTier.Copper, DiskSpec.For(DiskTier.Copper));

    [Fact]
    public void WholeItemsCountTowardItemsAndKinds()
    {
        var d = Copper();
        Assert.Equal(1, d.AddWhole("(W)4", 1));
        Assert.Equal(1, d.TotalItems);
        Assert.Equal(1, d.KindsUsed);
        Assert.True(d.HasKind("(W)4"));
    }

    [Fact]
    public void AWholeItemAndAPlainStackOfTheSameIdShareOneKind()
    {
        var d = Copper();
        d.Add(new ItemKey("(O)348"), 5);   // plain stack
        d.AddWhole("(O)348", 2);          // same base ID with its own state
        Assert.Equal(1, d.KindsUsed);
        Assert.Equal(7, d.TotalItems);
    }

    [Fact]
    public void TakingWholeItemsFreesSpaceAndTheKind()
    {
        var d = Copper();
        d.AddWhole("(O)344", 3);
        Assert.Equal(3, d.TakeWhole("(O)344", 5));
        Assert.Equal(0, d.TotalItems);
        Assert.Equal(0, d.KindsUsed);
    }

    [Fact]
    public void WholeItemsRespectTheKindLimit()
    {
        var d = Copper();
        for (int i = 0; i < d.Spec.MaxKinds; i++)
            d.Add(new ItemKey("(O)" + i), 1);
        Assert.Equal(0, d.AddWhole("(R)520", 1));
        Assert.Equal(1, d.AddWhole("(O)0", 1)); // existing kind still fits
    }

    [Fact]
    public void WholeItemsRespectTheItemLimit()
    {
        var d = Copper();
        d.Add(new ItemKey("(O)1"), d.Spec.MaxItems - 2);
        Assert.Equal(2, d.AddWhole("(O)1", 5));
        Assert.Equal(0, d.FreeItems);
    }

    [Fact]
    public void SetWholeRecountsWithoutTouchingPlainStacks()
    {
        var d = Copper();
        d.Add(new ItemKey("(O)1"), 10);
        d.AddWhole("(W)4", 1);
        d.SetWhole(new[] { ("(W)4", 1L), ("(R)520", 2L) });
        Assert.Equal(13, d.TotalItems);
        Assert.Equal(3, d.KindsUsed);
        Assert.Equal(3, d.WholeItems);
    }
}
