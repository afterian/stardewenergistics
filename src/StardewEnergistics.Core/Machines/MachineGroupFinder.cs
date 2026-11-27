using StardewEnergistics.Core.Geometry;

namespace StardewEnergistics.Core.Machines;

/// <summary>Finds the group of identical machines touching a Machine Link's machine (orthogonally, chained).</summary>
public static class MachineGroupFinder
{
    /// <param name="machineIdAt">Qualified machine ID on a tile, or null if no machine.</param>
    /// <param name="limit">Safety cap on group size.</param>
    public static IReadOnlyList<TilePos> Group(TilePos start, Func<TilePos, string?> machineIdAt, int limit = 500)
    {
        string? id = machineIdAt(start);
        if (id == null)
            return Array.Empty<TilePos>();

        var seen = new HashSet<TilePos> { start };
        var order = new List<TilePos> { start };
        var queue = new Queue<TilePos>();
        queue.Enqueue(start);
        while (queue.Count > 0 && order.Count < limit)
        {
            foreach (var n in queue.Dequeue().Neighbours())
            {
                if (order.Count >= limit)
                    break;
                if (seen.Add(n) && machineIdAt(n) == id)
                {
                    order.Add(n);
                    queue.Enqueue(n);
                }
            }
        }
        return order;
    }
}
