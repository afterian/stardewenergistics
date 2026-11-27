namespace StardewEnergistics.Core.Graph;

/// <summary>One connected network inside a location.</summary>
public sealed record Network(string Id, BlockNode? Core, IReadOnlyList<BlockNode> Blocks)
{
    public IEnumerable<BlockNode> OfKind(BlockKind k) => Blocks.Where(b => b.Kind == k);
}

/// <summary>Result of building a location's graph: its networks and any layout problems.</summary>
public sealed record NetworkGraph(IReadOnlyList<Network> Networks, IReadOnlyList<GraphIssue> Issues);
