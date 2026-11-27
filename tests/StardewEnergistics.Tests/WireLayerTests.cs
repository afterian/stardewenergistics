using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Wires;

namespace StardewEnergistics.Tests;

public class WireLayerTests
{
    private static readonly TilePos Centre = new(5, 5);

    public static IEnumerable<object[]> AllMasks() => Enumerable.Range(0, 16).Select(m => new object[] { m });

    [Theory]
    [MemberData(nameof(AllMasks))]
    public void EveryMaskMatchesItsNeighbours(int expected)
    {
        var layer = new WireLayer();
        layer.TryPlace(Centre, WireKind.Copper);
        foreach (var d in DirectionExt.All)
        {
            if (WireMask.Has(expected, d))
                layer.TryPlace(Centre.Step(d), WireKind.Copper);
        }
        Assert.Equal(expected, layer.MaskAt(Centre));
    }

    [Fact]
    public void ArtListFramesMeanWhatTheySay()
    {
        Assert.Equal(5, WireMask.Of(Direction.North, Direction.South));   // straight N–S
        Assert.Equal(10, WireMask.Of(Direction.East, Direction.West));    // straight E–W
        Assert.Equal(15, WireMask.Of(Direction.North, Direction.East, Direction.South, Direction.West));
    }

    [Fact]
    public void NoWireMeansMaskZeroAndEmptyRun()
    {
        var layer = new WireLayer();
        layer.TryPlace(Centre.Step(Direction.North), WireKind.Copper);
        Assert.Equal(0, layer.MaskAt(Centre));
        Assert.Empty(layer.ConnectedRun(Centre));
    }

    [Fact]
    public void CantPlaceTwiceOrRemoveNothing()
    {
        var layer = new WireLayer();
        Assert.True(layer.TryPlace(Centre, WireKind.Copper));
        Assert.False(layer.TryPlace(Centre, WireKind.Heavy));
        Assert.Equal(WireKind.Copper, layer.Get(Centre)!.Kind);
        Assert.True(layer.Remove(Centre));
        Assert.False(layer.Remove(Centre));
        Assert.Null(layer.Get(Centre));
    }

    [Fact]
    public void RemovingTheMiddleOfARunSplitsIt()
    {
        var layer = Line(0, 6);
        Assert.Equal(7, layer.ConnectedRun(new TilePos(0, 0)).Count());

        layer.Remove(new TilePos(3, 0));

        Assert.Equal(3, layer.ConnectedRun(new TilePos(0, 0)).Count());
        Assert.Equal(3, layer.ConnectedRun(new TilePos(6, 0)).Count());
        Assert.Equal(WireMask.Of(Direction.West), layer.MaskAt(new TilePos(2, 0)));
        Assert.Equal(WireMask.Of(Direction.East), layer.MaskAt(new TilePos(4, 0)));
    }

    [Fact]
    public void PlacingInTheMiddleJoinsTwoRuns()
    {
        var layer = Line(0, 2);
        layer.TryPlace(new TilePos(4, 0), WireKind.Copper);
        Assert.Equal(3, layer.ConnectedRun(new TilePos(0, 0)).Count());

        layer.TryPlace(new TilePos(3, 0), WireKind.Copper);

        Assert.Equal(5, layer.ConnectedRun(new TilePos(0, 0)).Count());
        Assert.Equal(10, layer.MaskAt(new TilePos(3, 0)));
    }

    [Fact]
    public void DiagonalsDoNotConnect()
    {
        var layer = new WireLayer();
        layer.TryPlace(new TilePos(0, 0), WireKind.Copper);
        layer.TryPlace(new TilePos(1, 1), WireKind.Copper);
        Assert.Equal(0, layer.MaskAt(new TilePos(0, 0)));
        Assert.Single(layer.ConnectedRun(new TilePos(0, 0)));
    }

    [Fact]
    public void CopperAndHeavyCableJoin()
    {
        var layer = new WireLayer();
        layer.TryPlace(new TilePos(0, 0), WireKind.Copper);
        layer.TryPlace(new TilePos(1, 0), WireKind.Heavy);
        layer.TryPlace(new TilePos(2, 0), WireKind.Copper);
        Assert.Equal(10, layer.MaskAt(new TilePos(1, 0)));
        Assert.Equal(3, layer.ConnectedRun(new TilePos(2, 0)).Count());
    }

    [Fact]
    public void CoveredWireStillConnects()
    {
        var layer = Line(0, 2);
        layer.SetCovered(new TilePos(1, 0), true);
        Assert.Equal(10, layer.MaskAt(new TilePos(1, 0)));
        Assert.Equal(3, layer.ConnectedRun(new TilePos(0, 0)).Count());
    }

    [Fact]
    public void WrenchTapTogglesOneTile()
    {
        var layer = Line(0, 2);
        Assert.True(layer.ToggleCovered(new TilePos(1, 0)));
        Assert.True(layer.Get(new TilePos(1, 0))!.Covered);
        Assert.False(layer.Get(new TilePos(0, 0))!.Covered);
        Assert.False(layer.ToggleCovered(new TilePos(1, 0)));
        Assert.Null(layer.ToggleCovered(new TilePos(9, 9)));
    }

    [Fact]
    public void WrenchHoldCoversTheWholeRunOnly()
    {
        var layer = Line(0, 4);
        layer.TryPlace(new TilePos(10, 0), WireKind.Copper);
        layer.SetCovered(new TilePos(2, 0), true);

        Assert.Equal(4, layer.SetRunCovered(new TilePos(0, 0), true));

        Assert.All(Enumerable.Range(0, 5), x => Assert.True(layer.Get(new TilePos(x, 0))!.Covered));
        Assert.False(layer.Get(new TilePos(10, 0))!.Covered);
    }

    [Fact]
    public void OnlyHeavyCableCanBePainted()
    {
        var layer = new WireLayer();
        layer.TryPlace(new TilePos(0, 0), WireKind.Copper);
        layer.TryPlace(new TilePos(1, 0), WireKind.Heavy);

        Assert.False(layer.Paint(new TilePos(0, 0), 3));
        Assert.Null(layer.CycleColor(new TilePos(0, 0)));
        Assert.True(layer.Paint(new TilePos(1, 0), 3));
        Assert.Equal(3, layer.Get(new TilePos(1, 0))!.Color);
        Assert.Throws<ArgumentOutOfRangeException>(() => layer.Paint(new TilePos(1, 0), 20));
    }

    [Fact]
    public void ColourCycleWrapsAfter20()
    {
        var layer = new WireLayer();
        layer.TryPlace(Centre, WireKind.Heavy);
        layer.Paint(Centre, 19);
        Assert.Equal(0, layer.CycleColor(Centre));
        Assert.Equal(1, layer.CycleColor(Centre));
    }

    [Fact]
    public void ChangedFiresForEachEdit()
    {
        var layer = new WireLayer();
        var changes = new List<TilePos>();
        layer.Changed += changes.Add;

        layer.TryPlace(Centre, WireKind.Heavy);
        layer.TryPlace(Centre, WireKind.Heavy);   // no-op
        layer.SetCovered(Centre, true);
        layer.SetCovered(Centre, true);           // no-op
        layer.Paint(Centre, 4);
        layer.Remove(Centre);

        Assert.Equal(4, changes.Count);
    }

    private static WireLayer Line(int fromX, int toX)
    {
        var layer = new WireLayer();
        for (int x = fromX; x <= toX; x++)
            layer.TryPlace(new TilePos(x, 0), WireKind.Copper);
        return layer;
    }
}
