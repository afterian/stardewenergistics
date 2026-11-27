namespace StardewEnergistics.Core.Graph;

/// <summary>
/// How a non-rack block connects to wire. Open item: leaning <see cref="OwnTile"/>
/// (wire must be on the block's own tile); confirm with the user before M2.
/// </summary>
public enum ConnectionRule
{
    /// <summary>A block connects only to wire on its own tile.</summary>
    OwnTile,
    /// <summary>A block connects to wire on its own tile or any of the four neighbours.</summary>
    OwnTileOrNeighbours,
}
