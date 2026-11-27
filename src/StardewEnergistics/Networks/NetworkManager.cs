using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.World;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Networks;

/// <summary>A location's networks plus the game objects behind each block.</summary>
internal sealed class LocationNetworks
{
    public LocationNetworks(GameLocation location, NetworkGraph graph, Dictionary<string, SObject> objects)
    {
        Location = location;
        Graph = graph;
        Objects = objects;
    }

    public GameLocation Location { get; }

    public NetworkGraph Graph { get; }

    /// <summary>Block ID → placed object.</summary>
    public Dictionary<string, SObject> Objects { get; }

    public SObject ObjectOf(BlockNode b) => Objects[b.Id];

    /// <summary>The network of the block on a tile (ignoring blocks of other kinds), or null.</summary>
    public Network? NetworkAt(Vector2 tile) => Graph.NetworkAt(new TilePos((int)tile.X, (int)tile.Y));

    public IEnumerable<SObject> ObjectsOf(Network n, BlockKind kind) =>
        n.OfKind(kind).OrderBy(b => b.Pos.Y).ThenBy(b => b.Pos.X).Select(ObjectOf);
}

/// <summary>
/// Builds and caches each location's network graph. A location is rebuilt when its objects
/// change, its wire changes, or code calls <see cref="Invalidate"/> (e.g. a Chest Link was added).
/// </summary>
internal sealed class NetworkManager
{
    private readonly WireLayerStore wires;
    private readonly NetworkGraphBuilder builder = new(ConnectionRule.OwnTile);
    private readonly Dictionary<string, LocationNetworks> cache = new();
    private readonly HashSet<string> hookedLayers = new();

    public NetworkManager(WireLayerStore wires) => this.wires = wires;

    public void Register(IModHelper helper)
    {
        helper.Events.World.ObjectListChanged += (_, e) => Invalidate(e.Location);
        helper.Events.GameLoop.SaveLoaded += (_, _) => Clear();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Clear();
    }

    public void Clear()
    {
        cache.Clear();
        hookedLayers.Clear();
    }

    public void Invalidate(GameLocation location) => cache.Remove(WireLayerStore.KeyOf(location));

    public LocationNetworks For(GameLocation location)
    {
        string key = WireLayerStore.KeyOf(location);
        if (cache.TryGetValue(key, out var cached))
            return cached;

        var layer = wires.For(location);
        if (hookedLayers.Add(key))
            layer.Changed += _ => cache.Remove(key);

        var blocks = new List<BlockNode>();
        var objects = new Dictionary<string, SObject>();
        foreach (var (tile, obj) in location.objects.Pairs)
        {
            if (BlockIds.KindOf(obj) is not { } kind)
                continue;
            string id = $"{key}@{(int)tile.X},{(int)tile.Y}";
            blocks.Add(new BlockNode(id, kind, new TilePos((int)tile.X, (int)tile.Y)));
            objects[id] = obj;
        }

        var result = new LocationNetworks(location, builder.Build(layer, blocks), objects);
        cache[key] = result;
        return result;
    }

    /// <summary>Every loaded location that has at least one Computer Core.</summary>
    public IEnumerable<LocationNetworks> AllWithCores()
    {
        var list = new List<LocationNetworks>();
        Utility.ForEachLocation(location =>
        {
            if (location.objects.Values.Any(o => o.QualifiedItemId == "(BC)" + Ids.ComputerCore))
                list.Add(For(location));
            return true;
        });
        return list;
    }
}
