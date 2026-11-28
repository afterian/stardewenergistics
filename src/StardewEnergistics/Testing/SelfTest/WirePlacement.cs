using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// Where wire goes, with a mouse (cursor shown, within reach) and a controller (cursor hidden, the tile in
/// front): empty floor, under a chest, both halves of a Furnace, next to an object, flooring and tilled soil
/// take it; a wall, a tree and water refuse it with the reason. The outline's tile (WireInput.WireTile on the
/// same cursor) must be the tile that gets the wire.
/// </summary>
internal static class WirePlacement
{
    private sealed record Case(string Name, Vector2 Target, Vector2? WireAt, string? Refusal);

    public static IEnumerable<object?> Run(SelfTestContext c)
    {
        var loc = c.Loc;
        c.Place("(BC)130", c.T(1, 1));                 // chest
        c.Place("(BC)13", c.T(3, 1));                  // Furnace (top half drawn on (3,0))
        c.Place("(BC)130", c.T(5, 1));                 // an object to stand wire next to
        loc.terrainFeatures[c.T(7, 1)] = new Flooring("0");
        loc.terrainFeatures[c.T(8, 1)] = new HoeDirt(0, loc);
        loc.terrainFeatures[c.T(8, 3)] = new Tree("1", 5);
        var wall = c.T(0, -1);
        while (wall.Y > 0 && loc.isTilePassable(new xTile.Dimensions.Location((int)wall.X, (int)wall.Y), Game1.viewport))
            wall.Y--;

        var cases = new List<Case>
        {
            new("empty floor", c.T(0, 3), c.T(0, 3), null),
            new("under a chest", c.T(1, 1), c.T(1, 1), null),
            new("a Furnace's base", c.T(3, 1), c.T(3, 1), null),
            new("a Furnace's top half", c.T(3, 0), c.T(3, 1), null),
            new("next to an object", c.T(6, 1), c.T(6, 1), null),
            new("flooring", c.T(7, 1), c.T(7, 1), null),
            new("tilled soil", c.T(8, 1), c.T(8, 1), null),
            new("a tree", c.T(8, 3), null, "blocked"),
        };
        if (!loc.isTilePassable(new xTile.Dimensions.Location((int)wall.X, (int)wall.Y), Game1.viewport))
            cases.Add(new("a wall", wall, null, "blocked"));
        else
            c.Log("no wall tile above the area; skipping the wall case", LogLevel.Info);

        var wireTile = c.S.InputHandlers.First(h => h.GetType().Name == "WireInput").GetType()
            .GetMethod("WireTile", BindingFlags.NonPublic | BindingFlags.Static);
        c.Require("WireInput.WireTile found (the outline's tile)", wireTile != null);

        foreach (bool mouse in new[] { true, false })
        {
            foreach (var k in cases)
                Try(c, k, mouse, wireTile!);
            yield return null;
        }

        if (!c.Throwaway)
        {
            c.Log("water case runs only in launched sessions (it visits the farm)", LogLevel.Info);
            yield break;
        }
        // Water: on the farm, from the shore.
        var farm = Game1.getFarm();
        (Vector2 Water, Vector2 Shore)? spot = null;
        int w = farm.Map.Layers[0].LayerWidth, h = farm.Map.Layers[0].LayerHeight;
        for (int y = 2; y < h - 2 && spot == null; y++)
            for (int x = 2; x < w - 2 && spot == null; x++)
            {
                if (!farm.isWaterTile(x, y))
                    continue;
                var shore = new Vector2(x, y + 1);
                if (c.IsFree(farm, shore) && farm.isTilePassable(shore))
                    spot = (new Vector2(x, y), shore);
            }
        if (spot is not { } s)
        {
            c.Log("no water with a shore below it on this farm; skipping the water case", LogLevel.Info);
            yield break;
        }
        foreach (object? step in c.Visit(farm, Vector2.Zero, s.Shore)) yield return step;
        foreach (bool mouse in new[] { true, false })
            Try(c, new Case("water", s.Water, null, "water"), mouse, wireTile!, s.Shore);
        foreach (object? step in c.GoHome()) yield return step;
    }

    private static void Try(SelfTestContext c, Case k, bool mouse, MethodInfo wireTile, Vector2? standAt = null)
    {
        string how = mouse ? "mouse" : "controller";
        c.Step($"{how}: wire on {k.Name}");
        var layer = c.S.Wires.For(c.Loc);
        if (k.WireAt is { } at)
            layer.Remove(SelfTestContext.P(at));
        int before = layer.Count;
        Game1.hudMessages.Clear();

        Vector2 grab;
        if (standAt is { } st)
        {
            c.StandAt(st, Game1.up);
        }
        else if (mouse)
        {
            // A few tiles away, so only the cursor (not the tile in front) can point at the target.
            var spot = new[] { new Vector2(0, 3), new Vector2(0, 2), new Vector2(-2, 0), new Vector2(2, 0), new Vector2(0, -2) }
                .Select(d => k.Target + d).FirstOrDefault(t => c.IsFree(c.Loc, t) && c.Loc.isTilePassable(t), k.Target + new Vector2(0, 3));
            c.StandAt(spot, Game1.up);
        }
        else
        {
            // Right beside, facing it.
            foreach (var (d, facing) in new[] { (new Vector2(0, 1), Game1.up), (new Vector2(-1, 0), Game1.right), (new Vector2(1, 0), Game1.left), (new Vector2(0, -1), Game1.down) })
            {
                var t = k.Target + d;
                if (c.IsFree(c.Loc, t) && c.Loc.isTilePassable(t))
                {
                    c.StandAt(t, facing);
                    break;
                }
            }
        }
        grab = c.Player.GetGrabTile();
        if (!mouse && grab != k.Target)
        {
            c.Log($"controller: no free tile beside {k.Name} to face it from; skipped", LogLevel.Info);
            return;
        }
        c.Hold("(O)" + Ids.CopperWire, 10);
        var cursor = mouse ? k.Target : grab;
        bool taken = c.Input.Press(mouse ? SButton.MouseLeft : SButton.ControllerX, cursor, grab, mouse);
        var outline = wireTile.Invoke(null, new object[] { new FakeCursor(cursor, grab) });
        var outlineTile = outline is Core.Geometry.TilePos p ? new Vector2(p.X, p.Y) : new Vector2(-1, -1);

        if (k.WireAt is { } expected)
        {
            c.Check($"{how}: wire goes on {k.Name}", layer.Has(SelfTestContext.P(expected)) && layer.Count == before + 1,
                $"taken {taken}; wire now {layer.Count} (was {before}); {c.Messages()}");
            c.Check($"{how}: the outline shows the tile that got wire ({k.Name})", outlineTile == expected, $"outline {c.Rel(outlineTile)}, wire at {c.Rel(expected)}");
            c.CheckEqual($"{how}: one wire used ({k.Name})", 9, c.Carried("(O)" + Ids.CopperWire));
        }
        else
        {
            c.CheckEqual($"{how}: no wire on {k.Name}", before, layer.Count);
            c.Check($"{how}: says why not ({k.Name})", c.Showed(c.T("wire.cant." + k.Refusal)), c.Messages());
            c.Check($"{how}: the outline points at {k.Name}", outlineTile == k.Target, $"outline {c.Rel(outlineTile)}");
            c.CheckEqual($"{how}: no wire used ({k.Name})", 10, c.Carried("(O)" + Ids.CopperWire));
        }
    }
}
