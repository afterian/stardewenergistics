namespace StardewEnergistics.Core.Storage;

public enum DiskTier
{
    Copper,
    Iron,
    Gold,
    Iridium,
    Prismatic,
}

/// <summary>A disk's limits. <see cref="MaxKinds"/> = -1 means unlimited.</summary>
public sealed record DiskSpec(int MaxKinds, long MaxItems)
{
    public bool UnlimitedKinds => MaxKinds < 0;

    /// <summary>Default disk sizes from the design plan; config can override.</summary>
    public static DiskSpec For(DiskTier tier) => tier switch
    {
        DiskTier.Copper => new DiskSpec(20, 2_000),
        DiskTier.Iron => new DiskSpec(40, 8_000),
        DiskTier.Gold => new DiskSpec(63, 32_000),
        DiskTier.Iridium => new DiskSpec(63, 128_000),
        DiskTier.Prismatic => new DiskSpec(-1, 512_000),
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };
}

/// <summary>Stable ID of one disk item (stored in the disk's modData).</summary>
public readonly record struct DiskId(string Value)
{
    public override string ToString() => Value;
}
