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
    public LocationNetworks(NetworkManager manager, GameLocation location, NetworkGraph graph, Dictionary<string, SObject> objects)
    {
        Manager = manager;
        Location = location;
        Graph = graph;
        Objects = objects;
    }

    public NetworkManager Manager { get; }

    public GameLocation Location { get; }

    public NetworkGraph Graph { get; }

    /// <summary>Block ID → placed object.</summary>
    public Dictionary<string, SObject> Objects { get; }

    public SObject ObjectOf(BlockNode b) => Objects[b.Id];

    /// <summary>The network of the block on a tile (ignoring blocks of other kinds), or null.</summary>
    public Network? NetworkAt(Vector2 tile) => Graph.NetworkAt(new TilePos((int)tile.X, (int)tile.Y));

    public IEnumerable<SObject> ObjectsOf(Network n, BlockKind kind) =>
        n.OfKind(kind).OrderBy(b => b.Pos.Y).ThenBy(b => b.Pos.X).Select(ObjectOf);

    /// <summary>True if the network can run: it has its own Core, or a Network Port joins it to one.</summary>
    public bool IsActive(Network n) => n.Working || Manager.GroupOf(n)?.Working == true;
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
    public NetworkManager(WireLayerStore wires)
    {
        this.wires = wires;
        wires.TileChanged += (key, _) =>
        {
            cache.Remove(key);
            links = null;
        };
    }

    public void Register(IModHelper helper)
    {
        helper.Events.World.ObjectListChanged += (_, e) => Invalidate(e.Location);
        helper.Events.GameLoop.SaveLoaded += (_, _) => Clear();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Clear();
    }

    public void Clear()
    {
        cache.Clear();
        links = null;
    }

    public void Invalidate(GameLocation location)
    {
        cache.Remove(WireLayerStore.KeyOf(location));
        links = null;
    }

    public LocationNetworks For(GameLocation location)
    {
        string key = WireLayerStore.KeyOf(location);
        if (cache.TryGetValue(key, out var cached))
            return cached;

        var layer = wires.For(location);

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

        var result = new LocationNetworks(this, location, builder.Build(layer, blocks), objects);
        cache[key] = result;
        return result;
    }

    /// <summary>Every loaded location that has at least one Computer Core.</summary>
    public IEnumerable<LocationNetworks> AllWithCores()
    {
        var list = new List<LocationNetworks>();
        Utility.ForEachLocation(location =>
        {
            if (location.objects.Values.Any(o => o.QualifiedItemId is "(BC)" + Ids.ComputerCore or "(BC)" + Ids.NetworkPort or "(BC)" + Ids.IslandUplink))
                list.Add(For(location));
            return true;
        });
        return list;
    }

    // ---- Network Ports: farm-wide groups ----

    private LinkGroups? links;
    private Dictionary<string, (LocationNetworks Loc, Network Net)> byId = new();

    /// <summary>The farm-wide group a network belongs to.</summary>
    public LinkGroup? GroupOf(Network n) => Links().GroupOf(n.Id);

    /// <summary>Every network joined with this one (including itself), with its location.</summary>
    public IEnumerable<(LocationNetworks Loc, Network Net)> Members(LocationNetworks loc, Network n)
    {
        var group = GroupOf(n);
        if (group == null || group.Members.Count <= 1)
        {
            yield return (loc, n);
            yield break;
        }
        foreach (string id in group.Members)
            if (byId.TryGetValue(id, out var m))
                yield return m;
    }

    /// <summary>
    /// The network that holds this one's Core-side state (power, autocraft jobs): itself if it has a Core,
    /// else its group's primary network. Null if neither.
    /// </summary>
    public (LocationNetworks Loc, Network Net)? Home(LocationNetworks loc, Network n)
    {
        if (n.Working)
            return (loc, n);
        string? primary = GroupOf(n)?.Primary;
        return primary != null && byId.TryGetValue(primary, out var home) ? home : null;
    }

    /// <summary>The building a Network Port at this farm tile sits beside, if any.</summary>
    public static StardewValley.Buildings.Building? BuildingBeside(GameLocation outdoors, Vector2 tile)
    {
        int x = (int)tile.X, y = (int)tile.Y;
        foreach (var b in outdoors.buildings)
        {
            int bx = b.tileX.Value, by = b.tileY.Value, w = b.tilesWide.Value, h = b.tilesHigh.Value;
            bool inside = x >= bx && x < bx + w && y >= by && y < by + h;
            bool besideX = (x == bx - 1 || x == bx + w) && y >= by && y < by + h;
            bool besideY = (y == by - 1 || y == by + h) && x >= bx && x < bx + w;
            if (!inside && (besideX || besideY) && b.GetIndoors() != null)
                return b;
        }
        return null;
    }

    private LinkGroups Links()
    {
        if (links != null)
            return links;

        var nodes = new Dictionary<string, LinkNode>();
        var edges = new List<(string, string)>();
        byId = new Dictionary<string, (LocationNetworks, Network)>();

        void AddLocation(LocationNetworks l)
        {
            bool home = l.Location.NameOrUniqueName == "FarmHouse";
            foreach (var n in l.Graph.Networks)
            {
                nodes[n.Id] = new LinkNode(n.Id, n.Working, home);
                byId[n.Id] = (l, n);
            }
        }

        var locations = AllWithCores();
        foreach (var l in locations)
            AddLocation(l);

        foreach (var l in locations)
        {
            if (!l.Location.IsOutdoors)
                continue;
            foreach (var n in l.Graph.Networks)
            {
                foreach (var port in n.OfKind(BlockKind.Port))
                {
                    var building = BuildingBeside(l.Location, new Vector2(port.Pos.X, port.Pos.Y));
                    var indoors = building?.GetIndoors();
                    if (indoors == null)
                        continue;
                    var inside = For(indoors);
                    if (!byId.ContainsKey(inside.Graph.Networks.FirstOrDefault()?.Id ?? ""))
                        AddLocation(inside);
                    var target = inside.Graph.Networks.FirstOrDefault(x => x.Working);
                    if (target != null)
                        edges.Add((n.Id, target.Id));
                }
            }
        }

        // Island Uplinks: every network with an Uplink joins every other one (Ginger Island ↔ home farm).
        var uplinked = locations.SelectMany(l => l.Graph.Networks.Where(n => n.OfKind(BlockKind.Uplink).Any())).Select(n => n.Id).ToList();
        for (int i = 1; i < uplinked.Count; i++)
            edges.Add((uplinked[0], uplinked[i]));

        links = new LinkGroups(nodes.Values, edges);
        return links;
    }
}
