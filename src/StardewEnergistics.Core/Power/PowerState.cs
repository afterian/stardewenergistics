namespace StardewEnergistics.Core.Power;

/// <summary>Stored power for one network.</summary>
/// <param name="BatteryCharge">Charge left in each battery slot, in rack order.</param>
/// <param name="Fuel">Generator fuel loaded, counted in power units.</param>
/// <param name="LastWarningDay">Absolute day the low-power mail was last sent, or -1.</param>
public sealed record PowerState(IReadOnlyList<int> BatteryCharge, int Fuel, int LastWarningDay = -1);

/// <summary>Outcome of one day's power draw.</summary>
public sealed record PowerDayResult(PowerState After, bool Powered, int Shortfall);
