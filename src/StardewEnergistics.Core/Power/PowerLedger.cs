using StardewEnergistics.Core.Graph;

namespace StardewEnergistics.Core.Power;

/// <summary>Daily power maths: use, draw-down (batteries first, then generator fuel), days remaining.</summary>
public sealed class PowerLedger
{
    public int DailyUse(Network n, PowerTable t) => throw new NotImplementedException();

    public PowerDayResult RunDay(PowerState s, int use) => throw new NotImplementedException();

    /// <summary>Whole days the network can still run; drives the 3-day warning.</summary>
    public int DaysRemaining(PowerState s, int use) => throw new NotImplementedException();
}
