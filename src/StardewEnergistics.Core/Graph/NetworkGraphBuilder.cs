using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Wires;

namespace StardewEnergistics.Core.Graph;

/// <summary>
/// Builds a location's networks from its wire layer and placed blocks.
/// <list type="bullet">
/// <item>Non-rack blocks (Core, Terminal, Links, generator, port…) join the wire run on their tile
/// (or a neighbouring tile, depending on <see cref="Rule"/>).</item>
/// <item>Racks join only by touching the Core or another rack that does; wire doesn't connect racks.</item>
/// <item>A network needs exactly one Core. Two Cores disable it (<see cref="Network.Core"/> is null).</item>
/// </list>
/// </summary>
public sealed class NetworkGraphBuilder
{
    public NetworkGraphBuilder(ConnectionRule rule = ConnectionRule.OwnTile) => Rule = rule;

    public ConnectionRule Rule { get; }

    public NetworkGraph Build(WireLayer wires, IReadOnlyList<BlockNode> blocks)
    {
        var issues = new List<GraphIssue>();

        // Union-find over blocks (index 0..n-1) and wire runs (index n..).
        int n = blocks.Count;
        var runOf = LabelRuns(wires, out int runCount);
        var uf = new UnionFind(n + runCount);

        var byPos = new Dictionary<TilePos, List<int>>();
        for (int i = 0; i < n; i++)
        {
            if (!byPos.TryGetValue(blocks[i].Pos, out var list))
                byPos[blocks[i].Pos] = list = new List<int>();
            list.Add(i);
        }

        for (int i = 0; i < n; i++)
        {
            var b = blocks[i];
            if (b.Kind.IsRack())
            {
                // Racks join by touching the Core or another rack.
                foreach (var np in b.Pos.Neighbours())
                {
                    if (!byPos.TryGetValue(np, out var others))
                        continue;
                    foreach (int j in others)
                    {
                        if (blocks[j].Kind == BlockKind.Core || blocks[j].Kind.IsRack())
                            uf.Union(i, j);
                    }
                }
                continue;
            }

            foreach (var p in ConnectTiles(b.Pos))
            {
                if (runOf.TryGetValue(p, out int run))
                    uf.Union(i, n + run);
            }
        }

        // Group blocks by set.
        var groups = new Dictionary<int, List<BlockNode>>();
        var order = new List<int>();
        for (int i = 0; i < n; i++)
        {
            int root = uf.Find(i);
            if (!groups.TryGetValue(root, out var g))
            {
                groups[root] = g = new List<BlockNode>();
                order.Add(root);
            }
            g.Add(blocks[i]);
        }

        var networks = new List<Network>();
        foreach (int root in order)
        {
            var members = groups[root];
            var cores = members.Where(b => b.Kind == BlockKind.Core).OrderBy(b => b.Pos.Y).ThenBy(b => b.Pos.X).ToList();
            BlockNode? core = null;

            if (cores.Count == 1)
            {
                core = cores[0];
            }
            else if (cores.Count > 1)
            {
                foreach (var c in cores)
                    issues.Add(new GraphIssue(GraphIssueKind.TwoCores, c.Pos, c.Id));
            }
            else if (members.All(b => b.Kind.IsRack()))
            {
                foreach (var r in members)
                    issues.Add(new GraphIssue(GraphIssueKind.RackNotTouchingCore, r.Pos, r.Id));
            }
            else
            {
                var first = members.OrderBy(b => b.Pos.Y).ThenBy(b => b.Pos.X).First();
                issues.Add(new GraphIssue(GraphIssueKind.NoCore, first.Pos, first.Id));
            }

            string id = core?.Id ?? members.Select(b => b.Id).OrderBy(s => s, StringComparer.Ordinal).First();
            networks.Add(new Network(id, core, members));
        }

        return new NetworkGraph(networks, issues);
    }

    private IEnumerable<TilePos> ConnectTiles(TilePos p)
    {
        yield return p;
        if (Rule == ConnectionRule.OwnTileOrNeighbours)
        {
            foreach (var np in p.Neighbours())
                yield return np;
        }
    }

    /// <summary>Labels every wire tile with the index of its connected run.</summary>
    private static Dictionary<TilePos, int> LabelRuns(WireLayer wires, out int runCount)
    {
        var runOf = new Dictionary<TilePos, int>();
        runCount = 0;
        foreach (var (pos, _) in wires.Tiles)
        {
            if (runOf.ContainsKey(pos))
                continue;
            foreach (var p in wires.ConnectedRun(pos))
                runOf[p] = runCount;
            runCount++;
        }
        return runOf;
    }

    private sealed class UnionFind
    {
        private readonly int[] parent;

        public UnionFind(int size)
        {
            parent = new int[size];
            for (int i = 0; i < size; i++)
                parent[i] = i;
        }

        public int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        public void Union(int a, int b)
        {
            int ra = Find(a), rb = Find(b);
            if (ra != rb)
                parent[rb] = ra;
        }
    }
}
