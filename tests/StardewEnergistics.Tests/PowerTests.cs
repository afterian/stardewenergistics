using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Power;

namespace StardewEnergistics.Tests;

public class PowerTests
{
    private readonly PowerLedger ledger = new();

    private static Network StarterNetwork()
    {
        var blocks = new List<BlockNode>
        {
            new("core", BlockKind.Core, new TilePos(0, 0)),
            new("rack", BlockKind.StorageRack, new TilePos(1, 0)),
            new("term", BlockKind.Terminal, new TilePos(0, 2)),
        };
        for (int i = 0; i < 10; i++)
            blocks.Add(new BlockNode("link" + i, i < 5 ? BlockKind.ChestLink : BlockKind.MachineLink, new TilePos(i, 5)));
        return new Network("core", blocks[0], blocks);
    }

    [Fact]
    public void StarterNetworkUsesSevenADay()
    {
        Assert.Equal(7, ledger.DailyUse(StarterNetwork(), PowerTable.Default));
    }

    [Fact]
    public void OneBatteryRunsAStarterNetworkTwoWeeks()
    {
        int use = ledger.DailyUse(StarterNetwork(), PowerTable.Default);
        var s = ledger.AddBattery(PowerState.Empty, PowerTable.Default);

        int poweredDays = 0;
        for (int day = 0; day < 28; day++)
        {
            var r = ledger.RunDay(s, use);
            s = r.After;
            if (r.Powered)
                poweredDays++;
        }

        Assert.Equal(14, poweredDays);
        Assert.Equal(2, s.Available); // 100 - 14 * 7
    }

    [Fact]
    public void BatteriesDrainInSlotOrderThenFuel()
    {
        var s = new PowerState(new[] { 5, 10 }, Fuel: 20);

        var r = ledger.RunDay(s, 12);
        Assert.True(r.Powered);
        Assert.Equal(new[] { 0, 3 }, r.After.BatteryCharge);
        Assert.Equal(20, r.After.Fuel);

        r = ledger.RunDay(r.After, 12);
        Assert.Equal(new[] { 0, 0 }, r.After.BatteryCharge);
        Assert.Equal(11, r.After.Fuel); // generator burns only once batteries are empty
    }

    [Fact]
    public void ShortDayDrawsNothingAndPauses()
    {
        var s = new PowerState(new[] { 3 }, Fuel: 2);
        var r = ledger.RunDay(s, 7);
        Assert.False(r.Powered);
        Assert.Equal(2, r.Shortfall);
        Assert.Equal(s, r.After);
    }

    [Fact]
    public void CoalValueIsConfigurable()
    {
        var oneEach = ledger.AddFuel(PowerState.Empty, PowerTable.Default, coal: 10, hardwood: 2);
        Assert.Equal(12, oneEach.Fuel);

        var tenEach = ledger.AddFuel(PowerState.Empty, PowerTable.Default with { CoalPower = 10 }, coal: 10, hardwood: 0);
        Assert.Equal(100, tenEach.Fuel);
    }

    [Fact]
    public void DaysRemainingCountsWholeDays()
    {
        Assert.Equal(14, ledger.DaysRemaining(new PowerState(new[] { 100 }), 7));
        Assert.Equal(0, ledger.DaysRemaining(new PowerState(new[] { 6 }), 7));
        Assert.Equal(int.MaxValue, ledger.DaysRemaining(PowerState.Empty, 0));
    }

    [Fact]
    public void WarningMailsOnceThreeDaysAheadAndResetsAfterRefill()
    {
        var s = new PowerState(new[] { 28 }); // 4 days at 7/day
        var (s1, warn1) = ledger.CheckWarning(s, 7, today: 10);
        Assert.False(warn1);

        s1 = ledger.RunDay(s1, 7).After; // 21 left = 3 days
        var (s2, warn2) = ledger.CheckWarning(s1, 7, today: 11);
        Assert.True(warn2);
        Assert.Equal(11, s2.LastWarningDay);

        s2 = ledger.RunDay(s2, 7).After;
        var (s3, warn3) = ledger.CheckWarning(s2, 7, today: 12);
        Assert.False(warn3); // already warned for this drain

        s3 = ledger.AddBattery(s3, PowerTable.Default);
        var (s4, warn4) = ledger.CheckWarning(s3, 7, today: 13);
        Assert.False(warn4);
        Assert.Equal(-1, s4.LastWarningDay);
    }
}
