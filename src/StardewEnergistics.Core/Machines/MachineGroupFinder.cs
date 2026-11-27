using StardewEnergistics.Core.Geometry;

namespace StardewEnergistics.Core.Machines;

/// <summary>Finds the group of identical machines touching a Machine Link's machine.</summary>
public static class MachineGroupFinder
{
    /// <param name="machineIdAt">Qualified machine ID on a tile, or null if no machine.</param>
    public static IReadOnlyList<TilePos> Group(TilePos start, Func<TilePos, string?> machineIdAt) => throw new NotImplementedException();
}
