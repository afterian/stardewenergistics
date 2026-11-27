using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Tests;

public class MachineRuleTests
{
    private sealed class Stock : IStockView
    {
        private readonly Dictionary<ItemKey, long> items = new();

        public Stock Add(ItemKey k, long n)
        {
            items[k] = n;
            return this;
        }

        public long Count(ItemKey k) => items.TryGetValue(k, out long n) ? n : 0;

        public IEnumerable<(ItemKey Key, long Count)> All() => items.Select(kv => (kv.Key, kv.Value));
    }

    private static readonly ItemKey AncientFruit = new("(O)454");
    private static readonly ItemKey Starfruit = new("(O)268");
    private static readonly ItemKey StarfruitGold = new("(O)268", 2);
    private static readonly ItemKey Hops = new("(O)304");

    private static LinkSettings Settings(InputRule[] inputs, KeepBackRule[]? keep = null) =>
        new(inputs, keep ?? Array.Empty<KeepBackRule>(), OutputMode.ToStorage, Schedule.AlwaysOn, Configured: true);

    [Fact]
    public void NewLinkDoesNothing()
    {
        var stock = new Stock().Add(Starfruit, 10);
        Assert.Null(InputSelector.Choose(LinkSettings.New, stock, _ => true));
    }

    [Fact]
    public void InputsArePickedInPriorityOrder()
    {
        var s = Settings(new[] { new InputRule(AncientFruit), new InputRule(Starfruit) });
        Assert.Equal(Starfruit, InputSelector.Choose(s, new Stock().Add(Starfruit, 5), _ => true));
        Assert.Equal(AncientFruit, InputSelector.Choose(s, new Stock().Add(Starfruit, 5).Add(AncientFruit, 1), _ => true));
    }

    [Fact]
    public void KeepBackIsNeverUsed()
    {
        var s = Settings(new[] { new InputRule(Hops) }, new[] { new KeepBackRule(Hops, 20) });
        Assert.Null(InputSelector.Choose(s, new Stock().Add(Hops, 20), _ => true));
        Assert.Equal(Hops, InputSelector.Choose(s, new Stock().Add(Hops, 21), _ => true));
        Assert.Equal(5, InputSelector.Usable(s, new Stock().Add(Hops, 25), Hops));
    }

    [Fact]
    public void KeepBackCoversAllQualities()
    {
        var s = Settings(new[] { new InputRule(Starfruit) }, new[] { new KeepBackRule(Starfruit, 10) });
        var stock = new Stock().Add(Starfruit, 6).Add(StarfruitGold, 6);
        Assert.Equal(2, InputSelector.Usable(s, stock, Starfruit));
        Assert.Equal(Starfruit, InputSelector.Choose(s, stock, _ => true)); // lower quality first
    }

    [Fact]
    public void OnlyAcceptedItemsAreChosen()
    {
        var s = Settings(new[] { new InputRule(AncientFruit), new InputRule(Starfruit) });
        var stock = new Stock().Add(Starfruit, 5).Add(AncientFruit, 5);
        Assert.Equal(Starfruit, InputSelector.Choose(s, stock, k => k.QualifiedId != AncientFruit.QualifiedId));
    }

    [Fact]
    public void NeededCountIsRespected()
    {
        var s = Settings(new[] { new InputRule(Hops) });
        Assert.Null(InputSelector.Choose(s, new Stock().Add(Hops, 4), _ => true, needed: 5));
    }

    [Fact]
    public void ExactQualityRuleOnlyMatchesThatQuality()
    {
        var s = Settings(new[] { new InputRule(StarfruitGold, AnyQuality: false) });
        Assert.Null(InputSelector.Choose(s, new Stock().Add(Starfruit, 50), _ => true));
        Assert.Equal(StarfruitGold, InputSelector.Choose(s, new Stock().Add(StarfruitGold, 1), _ => true));
    }

    [Fact]
    public void GroupFindsARowOfThirtyKegsButNotOtherMachines()
    {
        var map = new Dictionary<TilePos, string>();
        for (int x = 0; x < 30; x++)
            map[new TilePos(x, 0)] = "(BC)12"; // Keg
        map[new TilePos(30, 0)] = "(BC)15";   // Preserves Jar at the end
        map[new TilePos(5, 1)] = "(BC)12";    // a keg below joins too
        map[new TilePos(40, 0)] = "(BC)12";   // far away: not touching

        var group = MachineGroupFinder.Group(new TilePos(0, 0), p => map.TryGetValue(p, out var id) ? id : null);

        Assert.Equal(31, group.Count);
        Assert.DoesNotContain(new TilePos(30, 0), group);
        Assert.DoesNotContain(new TilePos(40, 0), group);
    }

    [Fact]
    public void GroupOfNothingIsEmpty() => Assert.Empty(MachineGroupFinder.Group(new TilePos(0, 0), _ => null));

    [Theory]
    [InlineData(true, 600, 2600, 300, true)]
    [InlineData(false, 900, 1700, 1200, true)]
    [InlineData(false, 900, 1700, 1700, false)]
    [InlineData(false, 2200, 800, 2400, true)]
    [InlineData(false, 2200, 800, 1200, false)]
    public void ScheduleWindows(bool always, int from, int to, int time, bool active) =>
        Assert.Equal(active, new Schedule(always, from, to).IsActive(time));
}

public class LinkSettingsCodecTests
{
    [Fact]
    public void SettingsRoundTrip()
    {
        var s = new StardewEnergistics.Core.Machines.LinkSettings(
            new[] { new StardewEnergistics.Core.Machines.InputRule(new StardewEnergistics.Core.Storage.ItemKey("(O)454")), new StardewEnergistics.Core.Machines.InputRule(new StardewEnergistics.Core.Storage.ItemKey("(O)268", 2), false) },
            new[] { new StardewEnergistics.Core.Machines.KeepBackRule(new StardewEnergistics.Core.Storage.ItemKey("(O)304"), 20) },
            StardewEnergistics.Core.Machines.OutputMode.LeaveInMachine,
            new StardewEnergistics.Core.Machines.Schedule(false, 2200, 800),
            true);

        var back = StardewEnergistics.Core.Persistence.LinkSettingsCodec.Decode(StardewEnergistics.Core.Persistence.LinkSettingsCodec.Encode(s));

        Assert.Equal(s.Inputs, back.Inputs);
        Assert.Equal(s.KeepBack, back.KeepBack);
        Assert.Equal(s.Output, back.Output);
        Assert.Equal(s.Schedule, back.Schedule);
        Assert.True(back.Configured);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("{not json")]
    public void BadSettingsGiveANewLink(string? json) =>
        Assert.False(StardewEnergistics.Core.Persistence.LinkSettingsCodec.Decode(json).Configured);
}
