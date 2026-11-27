namespace StardewEnergistics.Core.Geometry;

/// <summary>Orthogonal direction. Values are the wire-mask bits from the art list (N=1, E=2, S=4, W=8).</summary>
public enum Direction
{
    North = 1,
    East = 2,
    South = 4,
    West = 8,
}

public static class DirectionExt
{
    public static readonly IReadOnlyList<Direction> All = new[] { Direction.North, Direction.East, Direction.South, Direction.West };

    public static Direction Opposite(this Direction d) => d switch
    {
        Direction.North => Direction.South,
        Direction.East => Direction.West,
        Direction.South => Direction.North,
        Direction.West => Direction.East,
        _ => throw new ArgumentOutOfRangeException(nameof(d), d, null),
    };
}
