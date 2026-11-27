namespace StardewEnergistics.Core.Storage;

/// <summary>Read-only stock counts, used by machine input choice and crafting plans.</summary>
public interface IStockView
{
    long Count(ItemKey k);

    IEnumerable<(ItemKey Key, long Count)> All();
}
