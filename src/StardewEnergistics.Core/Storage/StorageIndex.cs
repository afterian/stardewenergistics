namespace StardewEnergistics.Core.Storage;

/// <summary>Item counts across a network's disks. Inserts fill disks in rack order.</summary>
public sealed class StorageIndex : IStockView
{
    public InsertResult Insert(ItemKey k, int n) => throw new NotImplementedException();

    public ExtractResult Extract(ItemKey k, int n) => throw new NotImplementedException();

    public long Count(ItemKey k) => throw new NotImplementedException();

    public IEnumerable<(ItemKey Key, long Count)> All() => throw new NotImplementedException();

    /// <summary>Kinds used on a disk; counts base QualifiedId only.</summary>
    public int KindsUsed(DiskId d) => throw new NotImplementedException();
}
