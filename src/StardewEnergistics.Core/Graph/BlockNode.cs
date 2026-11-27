using StardewEnergistics.Core.Geometry;

namespace StardewEnergistics.Core.Graph;

/// <summary>A placed network block, as the mod reports it to Core.</summary>
/// <param name="Id">Stable ID for this placed block (e.g. from the object's modData).</param>
public sealed record BlockNode(string Id, BlockKind Kind, TilePos Pos);
