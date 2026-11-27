using System.Collections.Generic;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Wires;
using StardewModdingAPI;
using StardewValley;

namespace StardewEnergistics.World;

/// <summary>
/// Holds the wire layer of every location. Saved with the farm by <see cref="SaveCoordinator"/>.
/// Keyed by <see cref="GameLocation.NameOrUniqueName"/>, which is unique for building interiors.
/// </summary>
internal sealed class WireLayerStore
{
    private readonly IMonitor monitor;
    private readonly Dictionary<string, WireLayer> layers = new();

    public WireLayerStore(IMonitor monitor) => this.monitor = monitor;

    public IEnumerable<KeyValuePair<string, WireLayer>> All => layers;

    public static string KeyOf(GameLocation location) => location.NameOrUniqueName;

    /// <summary>The location's layer, created empty if it has none yet.</summary>
    public WireLayer For(GameLocation location)
    {
        string key = KeyOf(location);
        if (!layers.TryGetValue(key, out var layer))
            layers[key] = layer = new WireLayer();
        return layer;
    }

    /// <summary>The location's layer, or null if it has no wire (cheap check for drawing).</summary>
    public WireLayer? Find(GameLocation location) => layers.TryGetValue(KeyOf(location), out var layer) ? layer : null;

    public void Load(SaveModel? model)
    {
        layers.Clear();
        if (model == null)
            return;

        int tiles = 0;
        foreach (var (key, dto) in model.Wires)
        {
            var layer = WireLayerMapper.FromDto(dto);
            layers[key] = layer;
            tiles += layer.Count;
        }
        monitor.Log($"Loaded {tiles} wire tiles in {layers.Count} locations.", LogLevel.Debug);
    }

    public void WriteTo(SaveModel model)
    {
        model.Wires.Clear();
        foreach (var (key, layer) in layers)
        {
            if (layer.Count > 0)
                model.Wires[key] = WireLayerMapper.ToDto(layer);
        }
    }

    public void Clear() => layers.Clear();
}
