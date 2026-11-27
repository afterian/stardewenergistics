using StardewEnergistics.Core.Geometry;

namespace StardewEnergistics.Core.Wires;

/// <summary>
/// The wire and cable floor layer of one game location. Copper Wire and Heavy Cable
/// join each other: anything touching the wire is connected, and there are no channels.
/// </summary>
public sealed class WireLayer
{
    private readonly Dictionary<TilePos, WireTile> tiles = new();

    /// <summary>Raised after a tile is placed, removed, covered/uncovered or painted.</summary>
    public event Action<TilePos>? Changed;

    public int Count => tiles.Count;

    public IEnumerable<KeyValuePair<TilePos, WireTile>> Tiles => tiles;

    /// <summary>Place a wire on an empty tile. Returns false if the tile already has wire.</summary>
    public bool TryPlace(TilePos p, WireKind k)
    {
        if (tiles.ContainsKey(p))
            return false;
        tiles[p] = new WireTile(k);
        Changed?.Invoke(p);
        return true;
    }

    /// <summary>Remove the wire on a tile. Returns false if there was none.</summary>
    public bool Remove(TilePos p)
    {
        if (!tiles.Remove(p))
            return false;
        Changed?.Invoke(p);
        return true;
    }

    public WireTile? Get(TilePos p) => tiles.TryGetValue(p, out var t) ? t : null;

    public bool Has(TilePos p) => tiles.ContainsKey(p);

    /// <summary>Connection mask 0..15 for a tile (N=1, E=2, S=4, W=8); equals the sprite frame index. 0 if no wire.</summary>
    public int MaskAt(TilePos p)
    {
        if (!tiles.ContainsKey(p))
            return WireMask.None;

        int mask = 0;
        foreach (var d in DirectionExt.All)
        {
            if (tiles.ContainsKey(p.Step(d)))
                mask |= (int)d;
        }
        return mask;
    }

    /// <summary>Every wire tile connected to <paramref name="start"/> (including it), for "hold the wrench to cover a run". Empty if no wire there.</summary>
    public IEnumerable<TilePos> ConnectedRun(TilePos start)
    {
        if (!tiles.ContainsKey(start))
            return Array.Empty<TilePos>();

        var seen = new HashSet<TilePos> { start };
        var queue = new Queue<TilePos>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var n in p.Neighbours())
            {
                if (tiles.ContainsKey(n) && seen.Add(n))
                    queue.Enqueue(n);
            }
        }
        return seen;
    }

    /// <summary>Cover or uncover one tile. Returns false if there is no wire or nothing changed.</summary>
    public bool SetCovered(TilePos p, bool covered)
    {
        if (!tiles.TryGetValue(p, out var t) || t.Covered == covered)
            return false;
        tiles[p] = t with { Covered = covered };
        Changed?.Invoke(p);
        return true;
    }

    /// <summary>Wrench tap: flip one tile's covered flag. Returns the new state, or null if no wire.</summary>
    public bool? ToggleCovered(TilePos p)
    {
        if (!tiles.TryGetValue(p, out var t))
            return null;
        SetCovered(p, !t.Covered);
        return !t.Covered;
    }

    /// <summary>Wrench hold: cover or uncover a whole connected run. Returns how many tiles changed.</summary>
    public int SetRunCovered(TilePos start, bool covered)
    {
        int changed = 0;
        foreach (var p in ConnectedRun(start).ToList())
        {
            if (SetCovered(p, covered))
                changed++;
        }
        return changed;
    }

    /// <summary>Paint a Heavy Cable tile (0..19). Copper Wire can't be painted. Returns false if nothing changed.</summary>
    public bool Paint(TilePos p, int color)
    {
        if (color < 0 || color >= WireTile.ColorCount)
            throw new ArgumentOutOfRangeException(nameof(color), color, $"Colour must be 0..{WireTile.ColorCount - 1}.");
        if (!tiles.TryGetValue(p, out var t) || t.Kind != WireKind.Heavy || t.Color == color)
            return false;
        tiles[p] = t with { Color = color };
        Changed?.Invoke(p);
        return true;
    }

    /// <summary>Wrench colour-cycle on Heavy Cable. Returns the new colour, or null if not Heavy Cable.</summary>
    public int? CycleColor(TilePos p)
    {
        if (!tiles.TryGetValue(p, out var t) || t.Kind != WireKind.Heavy)
            return null;
        int next = (t.Color + 1) % WireTile.ColorCount;
        Paint(p, next);
        return next;
    }

    /// <summary>Put a tile back exactly as saved (used by loading). Overwrites any existing tile.</summary>
    public void Restore(TilePos p, WireTile tile)
    {
        tiles[p] = tile.Kind == WireKind.Heavy ? tile : tile with { Color = 0 };
        Changed?.Invoke(p);
    }

    public void Clear()
    {
        var all = tiles.Keys.ToList();
        tiles.Clear();
        foreach (var p in all)
            Changed?.Invoke(p);
    }
}
