using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Tests;

public class StorageAndSaveContractTests
{
    [Theory]
    [InlineData(DiskTier.Copper, 20, 2_000)]
    [InlineData(DiskTier.Iron, 40, 8_000)]
    [InlineData(DiskTier.Gold, 63, 32_000)]
    [InlineData(DiskTier.Iridium, 63, 128_000)]
    [InlineData(DiskTier.Prismatic, -1, 512_000)]
    public void DiskSizesMatchTheDesignPlan(DiskTier tier, int kinds, long items)
    {
        var spec = DiskSpec.For(tier);
        Assert.Equal(kinds, spec.MaxKinds);
        Assert.Equal(items, spec.MaxItems);
        Assert.Equal(tier == DiskTier.Prismatic, spec.UnlimitedKinds);
    }

    [Fact]
    public void QualityAndFlavourMakeDifferentKeys()
    {
        Assert.NotEqual(new ItemKey("(O)24"), new ItemKey("(O)24", Quality: 2));
        Assert.NotEqual(new ItemKey("(O)344", PreserveId: "(O)613"), new ItemKey("(O)344", PreserveId: "(O)634"));
        Assert.Equal(new ItemKey("(O)24", 1), new ItemKey("(O)24", 1));
    }

    [Fact]
    public void NewerSaveSchemaIsRejected()
    {
        var model = new SaveModel { SchemaVersion = SaveModel.CurrentVersion + 1 };
        Assert.Throws<InvalidOperationException>(() => SaveMigrator.Migrate(model));
    }
}
