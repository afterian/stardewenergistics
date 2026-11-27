using StardewEnergistics.Core.Crafting;
using StardewEnergistics.Core.Storage;
using Xunit;

namespace StardewEnergistics.Tests;

public class StockKeeperTests
{
    private sealed class Stock : IStockView
    {
        public readonly Dictionary<ItemKey, long> Items = new();
        public long Count(ItemKey k) => Items.GetValueOrDefault(k);
        public IEnumerable<(ItemKey Key, long Count)> All() => Items.Select(kv => (kv.Key, kv.Value));
    }

    private static readonly ItemKey SpeedGro = new("(O)465");

    [Fact]
    public void MakesTheShortfallCountingAllQualitiesAndQueuedJobs()
    {
        var stock = new Stock();
        stock.Items[SpeedGro] = 30;
        stock.Items[SpeedGro with { Quality = 2 }] = 10;
        var queue = new JobQueue();
        var step = new CraftStep(new RecipeDef("Speed-Gro", new[] { new ItemAmount(new ItemKey("(O)766"), 5) }, new ItemAmount(SpeedGro, 5), RecipeKind.Craft), 4);
        queue.Add(new ItemAmount(SpeedGro, 20), new CraftPlan(new[] { step }, Array.Empty<ItemAmount>(), Array.Empty<ItemAmount>()));

        var due = StockKeeper.Due(new[] { new StockRule(SpeedGro, 100) }, stock, queue);
        Assert.Equal(new[] { new ItemAmount(SpeedGro, 40) }, due);
    }

    [Fact]
    public void MetRulesAreSkipped()
    {
        var stock = new Stock();
        stock.Items[SpeedGro] = 100;
        Assert.Empty(StockKeeper.Due(new[] { new StockRule(SpeedGro, 100) }, stock, new JobQueue()));
    }

    [Fact]
    public void LastRuleForAnItemWins()
    {
        var due = StockKeeper.Due(new[] { new StockRule(SpeedGro, 100), new StockRule(SpeedGro, 5) }, new Stock(), new JobQueue());
        Assert.Equal(5, Assert.Single(due).Count);
    }
}

public class StockRuleCodecTests
{
    [Fact]
    public void RoundTripsAndReplaces()
    {
        var rules = StockRuleCodec.With(new List<StockRule>(), new ItemKey("(O)465"), 100);
        rules = StockRuleCodec.With(rules, new ItemKey("(O)466"), 20);
        rules = StockRuleCodec.With(rules, new ItemKey("(O)465"), 50);
        var back = StockRuleCodec.Decode(StockRuleCodec.Encode(rules));
        Assert.Equal(2, back.Count);
        Assert.Contains(new StockRule(new ItemKey("(O)465"), 50), back);
        Assert.Empty(StockRuleCodec.With(back, new ItemKey("(O)465"), 0).Where(r => r.Item.QualifiedId == "(O)465"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("x;5;-1 (O)1;3 ")]
    public void BadInputIsSkipped(string? raw) => Assert.Empty(StockRuleCodec.Decode(raw));
}
