using System;
using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewValley;

namespace StardewEnergistics.UI;

/// <summary>Terminal data straight from network storage (host, or a split-screen farmhand sharing its memory).</summary>
internal sealed class LocalTerminalBackend : ITerminalBackend
{
    private readonly Func<(LocationNetworks Loc, StardewEnergistics.Core.Graph.Network Net)?> resolve;
    private readonly DiskStore disks;
    private readonly PowerService power;

    public LocalTerminalBackend(Func<(LocationNetworks, StardewEnergistics.Core.Graph.Network)?> resolve, DiskStore disks, PowerService power)
    {
        this.resolve = resolve;
        this.disks = disks;
        this.power = power;
    }

    public bool CanCraft => true;

    private NetworkStorage? Storage() => resolve() is { } r ? NetworkStorage.For(r.Loc, r.Net, disks) : null;

    public void Refresh(Action<IReadOnlyList<TerminalRow>, TerminalStatus> done)
    {
        var r = resolve();
        if (r == null)
        {
            done(Array.Empty<TerminalRow>(), new TerminalStatus(false, 0, 0, 0, 0, 0, 0, 0));
            return;
        }
        var storage = NetworkStorage.For(r.Value.Loc, r.Value.Net, disks);
        done(storage.Rows().Select(x => new TerminalRow(x, x.Display, x.Count)).ToList(), StatusOf(r.Value.Loc, r.Value.Net, storage, power));
    }

    public static TerminalStatus StatusOf(LocationNetworks loc, StardewEnergistics.Core.Graph.Network net, NetworkStorage storage, PowerService power)
    {
        var state = power.StateOf(loc, net);
        int use = power.DailyUse(loc, net);
        var d = storage.Disks.Disks;
        return new TerminalStatus(
            power.IsPowered(loc, net),
            state.Available,
            use <= 0 ? 999 : state.Available / use,
            d.Count,
            d.Sum(x => x.KindsUsed),
            d.Sum(x => x.Spec.UnlimitedKinds ? 0 : x.Spec.MaxKinds),
            d.Sum(x => x.TotalItems),
            d.Sum(x => x.Spec.MaxItems));
    }

    public void Take(TerminalRow row, int count, Action done)
    {
        var storage = Storage();
        if (storage != null && row.Handle is StoredRow stored)
        {
            var item = stored.Key is { } key ? storage.Take(key, count)
                : stored.DiskId is { } disk ? storage.TakeWhole(disk, stored.Display, count)
                : storage.TakeFromChest(stored.Chest!, stored.Display, count);
            if (item != null)
                TerminalItems.Give(item);
        }
        done();
    }

    // Put returns null when everything was stored; "?? item" here once gave the stored stack back (duplication).
    public void Put(Item item, Action<Item?> done)
    {
        Func<Item, Item?>? put = null;
        if (Storage() is { } storage)
            put = storage.Put;
        done(StoreAttempt.Leftover(item, put));
    }
}
