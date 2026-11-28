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
