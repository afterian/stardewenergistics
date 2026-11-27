using System;
using System.Collections.Generic;
using StardewEnergistics.Core.Storage;
using StardewValley;

namespace StardewEnergistics.UI;

/// <summary>One row the Terminal shows.</summary>
/// <param name="Handle">Opaque row reference for <see cref="ITerminalBackend.Take"/>.</param>
internal sealed record TerminalRow(object Handle, Item Display, long Count);

/// <summary>Summary numbers for the meters.</summary>
internal sealed record TerminalStatus(bool Powered, int PowerLeft, int DaysLeft, int Disks, int KindsUsed, int KindsMax, long ItemsUsed, long ItemsMax);

/// <summary>
/// Where a Terminal gets its data: directly from network storage (host and split-screen) or through
/// messages to the host (online farmhands). Results arrive through callbacks either way.
/// </summary>
internal interface ITerminalBackend
{
    /// <summary>Fetches rows and status, then calls <paramref name="done"/>.</summary>
    void Refresh(Action<IReadOnlyList<TerminalRow>, TerminalStatus> done);

    /// <summary>Moves up to <paramref name="count"/> of a row into the player's inventory, then calls <paramref name="done"/>.</summary>
    void Take(TerminalRow row, int count, Action done);

    /// <summary>Stores an item taken from the player's inventory. <paramref name="done"/> gets what didn't fit (or null).</summary>
    void Put(Item item, Action<Item?> done);

    /// <summary>True if this backend can open the crafting page.</summary>
    bool CanCraft { get; }
}

internal static class TerminalItems
{
    /// <summary>Gives an item to the player, dropping it at their feet if the inventory is full.</summary>
    public static void Give(Item item)
    {
        if (!Game1.player.addItemToInventoryBool(item))
            Game1.createItemDebris(item, Game1.player.getStandingPosition(), Game1.player.FacingDirection, Game1.player.currentLocation);
    }

    public static ItemKey KeyOf(string id, int quality) => new(id, quality);
}
