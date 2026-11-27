namespace StardewEnergistics.Core.Geometry;

/// <summary>A tile position inside one game location.</summary>
public readonly record struct TilePos(int X, int Y)
{
    public TilePos Step(Direction d) => d switch
    {
        Direction.North => new TilePos(X, Y - 1),
        Direction.East => new TilePos(X + 1, Y),
        Direction.South => new TilePos(X, Y + 1),
        Direction.West => new TilePos(X - 1, Y),
        _ => throw new ArgumentOutOfRangeException(nameof(d), d, null),
    };

    /// <summary>The four orthogonal neighbours, in N, E, S, W order.</summary>
    public IEnumerable<TilePos> Neighbours()
    {
        foreach (var d in DirectionExt.All)
            yield return Step(d);
    }

    public override string ToString() => $"({X}, {Y})";
}
