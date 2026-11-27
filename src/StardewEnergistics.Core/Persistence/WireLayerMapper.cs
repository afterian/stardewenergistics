using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Wires;

namespace StardewEnergistics.Core.Persistence;

/// <summary>Converts a <see cref="WireLayer"/> to and from its save DTO.</summary>
public static class WireLayerMapper
{
    public static WireLayerDto ToDto(WireLayer layer) => new()
    {
        Tiles = layer.Tiles
            .OrderBy(t => t.Key.Y).ThenBy(t => t.Key.X)
            .Select(t => new WireTileDto
            {
                X = t.Key.X,
                Y = t.Key.Y,
                Kind = t.Value.Kind.ToString(),
                Covered = t.Value.Covered,
                Color = t.Value.Color,
            })
            .ToList(),
    };

    /// <summary>Rebuilds a layer. Unknown kinds load as Copper; out-of-range colours load as 0.</summary>
    public static WireLayer FromDto(WireLayerDto? dto)
    {
        var layer = new WireLayer();
        if (dto?.Tiles == null)
            return layer;

        foreach (var t in dto.Tiles)
        {
            var kind = Enum.TryParse<WireKind>(t.Kind, ignoreCase: true, out var k) ? k : WireKind.Copper;
            int color = t.Color is >= 0 and < WireTile.ColorCount ? t.Color : 0;
            layer.Restore(new TilePos(t.X, t.Y), new WireTile(kind, t.Covered, color));
        }
        return layer;
    }
}
