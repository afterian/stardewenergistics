using System.Text.Json;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Wires;

namespace StardewEnergistics.Tests;

public class SaveModelTests
{
    [Fact]
    public void WireLayerSurvivesAJsonRoundTrip()
    {
        // A small shed layout: a copper run with a covered stretch, and a painted heavy cable spur.
        var layer = new WireLayer();
        for (int x = 2; x <= 8; x++)
            layer.TryPlace(new TilePos(x, 4), WireKind.Copper);
        for (int y = 5; y <= 7; y++)
            layer.TryPlace(new TilePos(5, y), WireKind.Heavy);
        layer.SetCovered(new TilePos(3, 4), true);
        layer.SetCovered(new TilePos(4, 4), true);
        layer.Paint(new TilePos(5, 6), 7);

        var model = new SaveModel();
        model.Wires["Shed"] = WireLayerMapper.ToDto(layer);
        string json = JsonSerializer.Serialize(model);
        var loaded = SaveMigrator.Migrate(JsonSerializer.Deserialize<SaveModel>(json)!);
        var restored = WireLayerMapper.FromDto(loaded.Wires["Shed"]);

        Assert.Equal(layer.Count, restored.Count);
        foreach (var (pos, tile) in layer.Tiles)
        {
            Assert.Equal(tile, restored.Get(pos));
            Assert.Equal(layer.MaskAt(pos), restored.MaskAt(pos));
        }
        Assert.Equal(WireMask.Of(Direction.East, Direction.South, Direction.West), restored.MaskAt(new TilePos(5, 4)));
    }

    [Fact]
    public void BadSaveValuesLoadSafely()
    {
        var dto = new WireLayerDto
        {
            Tiles =
            {
                new WireTileDto { X = 0, Y = 0, Kind = "Plasma", Color = 3 },
                new WireTileDto { X = 1, Y = 0, Kind = "heavy", Color = 99 },
            },
        };

        var layer = WireLayerMapper.FromDto(dto);

        Assert.Equal(new WireTile(WireKind.Copper, false, 0), layer.Get(new TilePos(0, 0)));
        Assert.Equal(new WireTile(WireKind.Heavy, false, 0), layer.Get(new TilePos(1, 0)));
        Assert.Equal(0, WireLayerMapper.FromDto(null).Count);
    }

    [Fact]
    public void EmptyModelRoundTrips()
    {
        var json = JsonSerializer.Serialize(new SaveModel());
        var model = SaveMigrator.Migrate(JsonSerializer.Deserialize<SaveModel>(json)!);
        Assert.Equal(SaveModel.CurrentVersion, model.SchemaVersion);
        Assert.Empty(model.Wires);
    }
}
