using System;
using System.Collections.Generic;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Wires;
using StardewModdingAPI;
using StardewValley;

namespace StardewEnergistics.World;

/// <summary>
/// Holds the wire layer of every location. Saved with the farm by <see cref="SaveCoordinator"/>.
/// Keyed by <see cref="GameLocation.NameOrUniqueName"/>, which is unique for building interiors.
/// Layer objects are never replaced once created, so listeners stay attached.
/// </summary>
internal sealed class WireLayerStore
{
    private readonly IMonitor monitor;
    private readonly Dictionary<string, WireLayer> layers = new();

    public WireLayerStore(IMonitor monitor) => this.monitor = monitor;

    /// <summary>Raised after any tile changes in any location: (location key, tile).</summary>
    public event Action<string, TilePos>? TileChanged;

    public IEnumerable<KeyValuePair<string, WireLayer>> All => layers;

    public static string KeyOf(GameLocation location) => location.NameOrUniqueName;

    /// <summary>The location's layer, created empty if it has none yet.</summary>
    public WireLayer For(GameLocation location) => For(KeyOf(location));

    public WireLayer For(string key)
    {
        if (!layers.TryGetValue(key, out var layer))
        {
            layers[key] = layer = new WireLayer();
            layer.Changed += p => TileChanged?.Invoke(key, p);
        }
        return layer;
    }

    /// <summary>The location's layer, or null if it has no wire (cheap check for drawing).</summary>
    public WireLayer? Find(GameLocation location) => layers.TryGetValue(KeyOf(location), out var layer) ? layer : null;

    /// <summary>Replaces all wire with the given layers (save load or a full sync from the host).</summary>
    public void Load(IReadOnlyDictionary<string, WireLayerDto>? wires)
    {
        Clear();
        if (wires == null)
            return;

        int tiles = 0;
        foreach (var (key, dto) in wires)
        {
            var layer = For(key);
            foreach (var (pos, tile) in WireLayerMapper.FromDto(dto).Tiles)
                layer.Restore(pos, tile);
            tiles += layer.Count;
        }
        monitor.Log($"Loaded {tiles} wire tiles in {layers.Count} locations.", LogLevel.Debug);
    }

    public void Load(SaveModel? model) => Load(model?.Wires);

    public void WriteTo(SaveModel model)
    {
        model.Wires.Clear();
        foreach (var (key, dto) in Snapshot())
            model.Wires[key] = dto;
    }

    /// <summary>All non-empty layers as DTOs.</summary>
    public Dictionary<string, WireLayerDto> Snapshot()
    {
        var result = new Dictionary<string, WireLayerDto>();
        foreach (var (key, layer) in layers)
        {
            if (layer.Count > 0)
                result[key] = WireLayerMapper.ToDto(layer);
        }
        return result;
    }

    public void Clear()
    {
        foreach (var layer in layers.Values)
            layer.Clear();
    }
}
