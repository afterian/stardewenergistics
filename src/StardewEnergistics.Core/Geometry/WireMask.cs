namespace StardewEnergistics.Core.Geometry;

/// <summary>
/// 4-bit connection mask used to pick a wire sprite frame: N=1, E=2, S=4, W=8.
/// Frame 0 = stub, 5 = straight N–S, 10 = straight E–W, 15 = cross.
/// </summary>
public static class WireMask
{
    public const int None = 0;
    public const int All = 15;

    public static int Of(params Direction[] dirs)
    {
        int mask = 0;
        foreach (var d in dirs)
            mask |= (int)d;
        return mask;
    }

    public static bool Has(int mask, Direction d) => (mask & (int)d) != 0;
}
