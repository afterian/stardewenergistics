namespace StardewEnergistics.Core.Power;

/// <summary>Stored power for one network.</summary>
/// <param name="BatteryCharge">Charge left in each Battery Pack, in rack slot order.</param>
/// <param name="Fuel">Generator fuel loaded, counted in power units.</param>
/// <param name="LastWarningDay">Day the low-power mail was sent for the current drain, or -1.</param>
public sealed record PowerState(IReadOnlyList<int> BatteryCharge, int Fuel = 0, int LastWarningDay = -1)
{
    public static readonly PowerState Empty = new(Array.Empty<int>());

    public int BatteryTotal => BatteryCharge.Sum();

    public int Available => BatteryTotal + Fuel;
}

/// <summary>Outcome of one day's power draw.</summary>
/// <param name="Powered">True if the full day's use was paid; machines and crafting run.</param>
/// <param name="Shortfall">Power missing on an unpowered day (nothing is drawn then).</param>
public sealed record PowerDayResult(PowerState After, bool Powered, int Shortfall);
