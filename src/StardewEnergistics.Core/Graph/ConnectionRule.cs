namespace StardewEnergistics.Core.Graph;

/// <summary>
/// How a non-rack block connects to wire. The mod uses <see cref="OwnTileOrNeighbours"/> by default
/// (playtest 2026-11-28: wire next to a block counts); <see cref="OwnTile"/> is a config option.
/// </summary>
public enum ConnectionRule
{
    /// <summary>A block connects only to wire on its own tile.</summary>
    OwnTile,
    /// <summary>A block connects to wire on its own tile or any of the four neighbours.</summary>
    OwnTileOrNeighbours,
}
