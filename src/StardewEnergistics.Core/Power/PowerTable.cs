using StardewEnergistics.Core.Graph;

namespace StardewEnergistics.Core.Power;

/// <summary>Daily power use per block kind, and fuel values. All values are config-tunable.</summary>
public sealed record PowerTable(
    IReadOnlyDictionary<BlockKind, int> DailyUse,
    int BatteryPower = 100,
    int CoalPower = 1,
    int HardwoodPower = 1)
{
    public int UseOf(BlockKind k) => DailyUse.TryGetValue(k, out int n) ? n : 0;
}
