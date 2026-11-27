using StardewEnergistics.Core.Geometry;

namespace StardewEnergistics.Core.Graph;

/// <summary>
/// One connected group of blocks inside a location. <see cref="Core"/> is null when the
/// network has no Core or more than one; such a network does nothing until fixed.
/// </summary>
public sealed record Network(string Id, BlockNode? Core, IReadOnlyList<BlockNode> Blocks)
{
    public bool Working => Core != null;

    public IEnumerable<BlockNode> OfKind(BlockKind k) => Blocks.Where(b => b.Kind == k);
}

/// <summary>Result of building a location's graph: its networks and any layout problems.</summary>
public sealed record NetworkGraph(IReadOnlyList<Network> Networks, IReadOnlyList<GraphIssue> Issues)
{
    /// <summary>The network a block belongs to, or null.</summary>
    public Network? NetworkOf(string blockId) => Networks.FirstOrDefault(n => n.Blocks.Any(b => b.Id == blockId));

    /// <summary>The network of a block on a tile, or null.</summary>
    public Network? NetworkAt(TilePos p) => Networks.FirstOrDefault(n => n.Blocks.Any(b => b.Pos == p));
}
