using StardewEnergistics.Core.Geometry;

namespace StardewEnergistics.Core.Graph;

public enum GraphIssueKind
{
    /// <summary>Two or more Cores on one connected network.</summary>
    TwoCores,
    /// <summary>Blocks are wired together but no Core is on the network.</summary>
    NoCore,
    /// <summary>A rack that touches neither the Core nor another rack.</summary>
    RackNotTouchingCore,
}

public sealed record GraphIssue(GraphIssueKind Kind, TilePos Pos, string? BlockId = null);
