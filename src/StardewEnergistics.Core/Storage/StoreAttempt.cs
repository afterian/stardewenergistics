namespace StardewEnergistics.Core.Storage;

/// <summary>
/// What's left after trying to store something. "Nothing left" (null) must stay null: treating it as
/// "no storage" handed the stored stack back to the player and duplicated it.
/// </summary>
public static class StoreAttempt
{
    /// <summary>
    /// The part of <paramref name="item"/> that didn't fit: all of it when there's no storage
    /// (<paramref name="put"/> is null), otherwise whatever <paramref name="put"/> returns (null when everything fit).
    /// </summary>
    public static T? Leftover<T>(T item, System.Func<T, T?>? put) where T : class =>
        put == null ? item : put(item);
}

/// <summary>Why an item didn't fit, for the message shown to the player.</summary>
public enum StoreRefusal
{
    CantStore,
    NoDisks,
    KindsFull,
    Full,
}

public static class StoreRefusals
{
    /// <summary>
    /// The reason an item was refused. "Kinds full" only applies to a new kind: when the kind is already on
    /// a disk, the item limit was the problem.
    /// </summary>
    public static StoreRefusal Why(bool storable, int disks, int kindsUsed, int kindsMax, bool kindAlreadyStored)
    {
        if (!storable)
            return StoreRefusal.CantStore;
        if (disks == 0)
            return StoreRefusal.NoDisks;
        if (!kindAlreadyStored && kindsMax > 0 && kindsUsed >= kindsMax)
            return StoreRefusal.KindsFull;
        return StoreRefusal.Full;
    }
}
