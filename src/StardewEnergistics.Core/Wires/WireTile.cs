namespace StardewEnergistics.Core.Wires;

/// <summary>One wire tile on a location's floor layer.</summary>
/// <param name="Kind">Copper Wire or Heavy Cable.</param>
/// <param name="Covered">Hidden by the Network Wrench; drawn only in Network View.</param>
/// <param name="Color">Heavy Cable paint, 0..19 (Stardew's chest colours, 0 = unpainted). Always 0 for Copper.</param>
public sealed record WireTile(WireKind Kind, bool Covered = false, int Color = 0)
{
    public const int ColorCount = 20;
}
