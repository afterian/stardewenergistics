using StardewEnergistics.Core.Geometry;

namespace StardewEnergistics.Core.Wires;

/// <summary>The wire and cable floor layer of one game location.</summary>
public sealed class WireLayer
{
    /// <summary>Place a wire on an empty tile. Returns false if the tile already has wire.</summary>
    public bool TryPlace(TilePos p, WireKind k) => throw new NotImplementedException();

    /// <summary>Remove the wire on a tile. Returns false if there was none.</summary>
    public bool Remove(TilePos p) => throw new NotImplementedException();

    public WireTile? Get(TilePos p) => throw new NotImplementedException();

    /// <summary>Connection mask 0..15 for a tile; equals the sprite frame index.</summary>
    public int MaskAt(TilePos p) => throw new NotImplementedException();

    /// <summary>Every wire tile connected to <paramref name="start"/>, for "hold the wrench to cover a run".</summary>
    public IEnumerable<TilePos> ConnectedRun(TilePos start) => throw new NotImplementedException();
}
