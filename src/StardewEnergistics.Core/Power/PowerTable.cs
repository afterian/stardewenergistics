using StardewEnergistics.Core.Graph;

namespace StardewEnergistics.Core.Power;

/// <summary>
/// Daily power use per block kind, and fuel values. All values are config-tunable.
/// Defaults: a starter network (Core, 1 Storage Rack, 1 Terminal, 10 Links) uses 7 a day,
/// so one Battery Pack (100) lasts 14 days.
/// </summary>
public sealed record PowerTable(
    IReadOnlyDictionary<BlockKind, double> DailyUse,
    int BatteryPower = 100,
    int CoalPower = 1,
    int HardwoodPower = 1)
{
    public static readonly IReadOnlyDictionary<BlockKind, double> DefaultUse = new Dictionary<BlockKind, double>
    {
        [BlockKind.Core] = 2,
        [BlockKind.StorageRack] = 1,
        [BlockKind.PowerRack] = 0,
        [BlockKind.CraftingRack] = 1,
        [BlockKind.AdvCraftingRack] = 2,
        [BlockKind.Terminal] = 1,
        [BlockKind.ChestLink] = 0.3,
        [BlockKind.MachineLink] = 0.3,
        [BlockKind.ShippingLink] = 0.3,
        [BlockKind.CoalGenerator] = 0,
        [BlockKind.PowerFeed] = 0,
        [BlockKind.Port] = 1,
        [BlockKind.Uplink] = 2,
    };

    public static readonly PowerTable Default = new(DefaultUse);

    public double UseOf(BlockKind k) => DailyUse.TryGetValue(k, out double n) ? n : 0;
}
