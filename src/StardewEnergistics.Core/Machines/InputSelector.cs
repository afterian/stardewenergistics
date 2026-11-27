using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Core.Machines;

/// <summary>Chooses the next item to feed a machine from storage, per a Link's settings.</summary>
public static class InputSelector
{
    /// <returns>The item to feed, or null when nothing allowed is in stock.</returns>
    public static ItemKey? Choose(LinkSettings s, IStockView stock, Func<ItemKey, bool> accepts) => throw new NotImplementedException();
}
