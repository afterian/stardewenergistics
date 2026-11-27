using StardewEnergistics.Core.Graph;

namespace StardewEnergistics.Core.Power;

/// <summary>
/// Daily power maths. Batteries pay first, in slot order; the Coal Generator burns only when
/// batteries are empty. An unpowered day draws nothing: storage still opens, machines and crafting pause.
/// </summary>
public sealed class PowerLedger
{
    public const int WarningDays = 3;

    /// <summary>A network's whole-number daily use (sum of block uses, rounded up).</summary>
    public int DailyUse(Network n, PowerTable t)
    {
        double sum = n.Blocks.Sum(b => t.UseOf(b.Kind));
        return (int)Math.Ceiling(Math.Round(sum, 6));
    }

    public PowerDayResult RunDay(PowerState s, int use)
    {
        if (use < 0)
            throw new ArgumentOutOfRangeException(nameof(use));
        if (use == 0)
            return new PowerDayResult(s, true, 0);
        if (s.Available < use)
            return new PowerDayResult(s, false, use - s.Available);

        var charge = s.BatteryCharge.ToArray();
        int left = use;
        for (int i = 0; i < charge.Length && left > 0; i++)
        {
            int take = Math.Min(charge[i], left);
            charge[i] -= take;
            left -= take;
        }
        int fuel = s.Fuel - left;
        return new PowerDayResult(s with { BatteryCharge = charge, Fuel = fuel }, true, 0);
    }

    /// <summary>Whole days the network can still run; drives the 3-day warning.</summary>
    public int DaysRemaining(PowerState s, int use) => use <= 0 ? int.MaxValue : s.Available / use;

    /// <summary>
    /// Decides whether to send the low-power mail today. Mails once per drain: the flag resets
    /// when power is topped up above the warning level again.
    /// </summary>
    public (PowerState State, bool SendWarning) CheckWarning(PowerState s, int use, int today)
    {
        int days = DaysRemaining(s, use);
        if (days > WarningDays)
            return (s.LastWarningDay == -1 ? s : s with { LastWarningDay = -1 }, false);
        if (s.LastWarningDay != -1)
            return (s, false);
        return (s with { LastWarningDay = today }, true);
    }

    /// <summary>Loads generator fuel. Returns the new state.</summary>
    public PowerState AddFuel(PowerState s, PowerTable t, int coal, int hardwood)
    {
        if (coal < 0 || hardwood < 0)
            throw new ArgumentOutOfRangeException(coal < 0 ? nameof(coal) : nameof(hardwood));
        return s with { Fuel = s.Fuel + coal * t.CoalPower + hardwood * t.HardwoodPower };
    }

    /// <summary>Adds a fresh Battery Pack to the next slot (a Power Rack holds 4).</summary>
    public PowerState AddBattery(PowerState s, PowerTable t) =>
        s with { BatteryCharge = s.BatteryCharge.Append(t.BatteryPower).ToArray() };
}
