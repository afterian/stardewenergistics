using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Wires;

namespace StardewEnergistics.Tests;

public class NetworkGraphTests
{
    /// <summary>
    /// A shed. Core at (2,2) with two Storage Racks and a Power Rack to its right.
    /// Copper wire from the Core runs right along y=4 then down; a Terminal and three Chest Links sit on the wire.
    /// <code>
    ///   y=2   C S S P
    ///   y=3   w
    ///   y=4   w w w w w w(T)
    ///   y=5             w(L1)
    ///   y=6             w(L2) w(L3)
    /// </code>
    /// </summary>
    private static (WireLayer, List<BlockNode>) Shed()
    {
        var w = new WireLayer();
        w.TryPlace(new TilePos(2, 2), WireKind.Copper); // under the Core
        w.TryPlace(new TilePos(2, 3), WireKind.Copper);
        for (int x = 2; x <= 7; x++)
            w.TryPlace(new TilePos(x, 4), WireKind.Copper);
        w.TryPlace(new TilePos(7, 5), WireKind.Copper);
        w.TryPlace(new TilePos(7, 6), WireKind.Copper);
        w.TryPlace(new TilePos(8, 6), WireKind.Copper);

        var blocks = new List<BlockNode>
        {
            new("core", BlockKind.Core, new TilePos(2, 2)),
            new("s1", BlockKind.StorageRack, new TilePos(3, 2)),
            new("s2", BlockKind.StorageRack, new TilePos(4, 2)),
            new("p1", BlockKind.PowerRack, new TilePos(5, 2)),
            new("term", BlockKind.Terminal, new TilePos(7, 4)),
            new("l1", BlockKind.ChestLink, new TilePos(7, 5)),
            new("l2", BlockKind.ChestLink, new TilePos(7, 6)),
            new("l3", BlockKind.ChestLink, new TilePos(8, 6)),
        };
        return (w, blocks);
    }

    [Fact]
    public void ShedLayoutIsOneWorkingNetwork()
    {
        var (w, blocks) = Shed();
        var g = new NetworkGraphBuilder().Build(w, blocks);

        Assert.Empty(g.Issues);
        var net = Assert.Single(g.Networks);
        Assert.True(net.Working);
        Assert.Equal("core", net.Core!.Id);
        Assert.Equal(8, net.Blocks.Count);
        Assert.Equal(2, net.OfKind(BlockKind.StorageRack).Count());
        Assert.Same(net, g.NetworkOf("l3"));
        Assert.Same(net, g.NetworkAt(new TilePos(7, 4)));
    }

    [Fact]
    public void TwoCoresOnOneNetworkIsAnError()
    {
        var (w, blocks) = Shed();
        w.TryPlace(new TilePos(9, 6), WireKind.Copper);
        blocks.Add(new BlockNode("core2", BlockKind.Core, new TilePos(9, 6)));

        var g = new NetworkGraphBuilder().Build(w, blocks);

        var net = Assert.Single(g.Networks);
        Assert.False(net.Working);
        Assert.Equal(2, g.Issues.Count(i => i.Kind == GraphIssueKind.TwoCores));
        Assert.Contains(g.Issues, i => i.BlockId == "core2");
    }

    [Fact]
    public void TwoSeparateShedsAreTwoNetworks()
    {
        var w = new WireLayer();
        w.TryPlace(new TilePos(0, 0), WireKind.Copper);
        w.TryPlace(new TilePos(1, 0), WireKind.Copper);
        w.TryPlace(new TilePos(10, 0), WireKind.Copper);
        w.TryPlace(new TilePos(11, 0), WireKind.Copper);
        var blocks = new List<BlockNode>
        {
            new("a", BlockKind.Core, new TilePos(0, 0)),
            new("at", BlockKind.Terminal, new TilePos(1, 0)),
            new("b", BlockKind.Core, new TilePos(10, 0)),
            new("bt", BlockKind.Terminal, new TilePos(11, 0)),
        };

        var g = new NetworkGraphBuilder().Build(w, blocks);

        Assert.Empty(g.Issues);
        Assert.Equal(2, g.Networks.Count);
        Assert.NotSame(g.NetworkOf("at"), g.NetworkOf("bt"));
    }

    [Fact]
    public void TerminalWithoutCoreReportsNoCore()
    {
        var w = new WireLayer();
        w.TryPlace(new TilePos(0, 0), WireKind.Copper);
        var g = new NetworkGraphBuilder().Build(w, new[] { new BlockNode("t", BlockKind.Terminal, new TilePos(0, 0)) });

        Assert.False(Assert.Single(g.Networks).Working);
        Assert.Equal(GraphIssueKind.NoCore, Assert.Single(g.Issues).Kind);
    }

    [Fact]
    public void RackOnWireJoinsTheNetwork()
    {
        var (w, blocks) = Shed();
        // A rack sitting on the wire at (4,4), away from the Core, joins through the wire.
        blocks.Add(new BlockNode("wired", BlockKind.StorageRack, new TilePos(4, 4)));

        var g = new NetworkGraphBuilder().Build(w, blocks);

        Assert.Empty(g.Issues);
        Assert.Contains(g.NetworkOf("core")!.Blocks, b => b.Id == "wired");
    }

    [Fact]
    public void RackBesideWireJoinsWithTheNeighbourRule()
    {
        var (w, blocks) = Shed();
        blocks.Add(new BlockNode("beside", BlockKind.StorageRack, new TilePos(4, 5))); // wire is at (4,4)

        Assert.DoesNotContain(new NetworkGraphBuilder(ConnectionRule.OwnTile).Build(w, blocks).NetworkOf("core")!.Blocks, b => b.Id == "beside");
        Assert.Contains(new NetworkGraphBuilder(ConnectionRule.OwnTileOrNeighbours).Build(w, blocks).NetworkOf("core")!.Blocks, b => b.Id == "beside");
    }

    [Fact]
    public void RackAwayFromTheCoreAndWireReportsAnIssue()
    {
        var (w, blocks) = Shed();
        blocks.Add(new BlockNode("lonely", BlockKind.StorageRack, new TilePos(12, 12)));

        var g = new NetworkGraphBuilder().Build(w, blocks);

        var issue = Assert.Single(g.Issues);
        Assert.Equal(GraphIssueKind.RackNotTouchingCore, issue.Kind);
        Assert.Equal("lonely", issue.BlockId);
        Assert.DoesNotContain(g.NetworkOf("core")!.Blocks, b => b.Id == "lonely");
    }

    [Fact]
    public void RacksChainThroughEachOther()
    {
        var blocks = new List<BlockNode> { new("core", BlockKind.Core, new TilePos(0, 0)) };
        for (int x = 1; x <= 6; x++)
            blocks.Add(new BlockNode("r" + x, BlockKind.StorageRack, new TilePos(x, 0)));
        blocks.Add(new BlockNode("left", BlockKind.PowerRack, new TilePos(-1, 0)));

        var g = new NetworkGraphBuilder().Build(new WireLayer(), blocks);

        Assert.Empty(g.Issues);
        Assert.Equal(8, Assert.Single(g.Networks).Blocks.Count);
    }

    [Fact]
    public void RacksDiagonalToTheCoreDoNotJoin()
    {
        var blocks = new List<BlockNode>
        {
            new("core", BlockKind.Core, new TilePos(0, 0)),
            new("r", BlockKind.StorageRack, new TilePos(1, 1)),
        };
        var g = new NetworkGraphBuilder().Build(new WireLayer(), blocks);
        Assert.Equal(GraphIssueKind.RackNotTouchingCore, Assert.Single(g.Issues).Kind);
    }

    [Fact]
    public void OwnTileRuleNeedsWireUnderTheBlock()
    {
        var w = new WireLayer();
        w.TryPlace(new TilePos(0, 0), WireKind.Copper);
        w.TryPlace(new TilePos(1, 0), WireKind.Copper);
        var blocks = new List<BlockNode>
        {
            new("core", BlockKind.Core, new TilePos(0, 0)),
            new("link", BlockKind.ChestLink, new TilePos(2, 0)), // wire ends beside it, not under it
        };

        var ownTile = new NetworkGraphBuilder(ConnectionRule.OwnTile).Build(w, blocks);
        var neighbours = new NetworkGraphBuilder(ConnectionRule.OwnTileOrNeighbours).Build(w, blocks);

        Assert.Equal(2, ownTile.Networks.Count);
        Assert.Contains(ownTile.Issues, i => i.Kind == GraphIssueKind.NoCore && i.BlockId == "link");
        Assert.Single(neighbours.Networks);
        Assert.Empty(neighbours.Issues);
    }

    [Fact]
    public void HeavyCableCarriesTheNetworkToo()
    {
        var w = new WireLayer();
        w.TryPlace(new TilePos(0, 0), WireKind.Copper);
        w.TryPlace(new TilePos(1, 0), WireKind.Heavy);
        w.TryPlace(new TilePos(2, 0), WireKind.Heavy);
        var blocks = new List<BlockNode>
        {
            new("core", BlockKind.Core, new TilePos(0, 0)),
            new("port", BlockKind.Port, new TilePos(2, 0)),
        };
        Assert.Single(new NetworkGraphBuilder().Build(w, blocks).Networks);
    }

    [Fact]
    public void EmptyLocationHasNoNetworks()
    {
        var w = new WireLayer();
        w.TryPlace(new TilePos(0, 0), WireKind.Copper);
        var g = new NetworkGraphBuilder().Build(w, Array.Empty<BlockNode>());
        Assert.Empty(g.Networks);
        Assert.Empty(g.Issues);
    }
}
