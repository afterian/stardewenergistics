using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewEnergistics.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewEnergistics.Multiplayer;

/// <summary>
/// Terminal requests from online farmhands, answered by the host. Only plain stackable items
/// (the ones disks hold) travel over the network; other chest items stay host-side for now.
/// </summary>
internal sealed class TerminalNet
{
    private const string RefreshType = "TermRefresh", RowsType = "TermRows";
    private const string TakeType = "TermTake", GivenType = "TermGiven";
    private const string PutType = "TermPut", LeftType = "TermLeft";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;
    private readonly string modId;

    private int nextRequest;
    private readonly Dictionary<int, Action<object>> pending = new();

    public TerminalNet(IModHelper helper, IMonitor monitor, NetworkManager networks, DiskStore disks, PowerService power, string modId)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
        this.modId = modId;
    }

    public sealed class Request
    {
        public int Id { get; set; }
        public string Location { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public string ItemId { get; set; } = "";
        public int Quality { get; set; }
        public int Count { get; set; }
    }

    public sealed class RowDto
    {
        public string ItemId { get; set; } = "";
        public int Quality { get; set; }
        public long Count { get; set; }
    }

    public sealed class RowsReply
    {
        public int Id { get; set; }
        public List<RowDto> Rows { get; set; } = new();
        public TerminalStatus? Status { get; set; }
    }

    public sealed class CountReply
    {
        public int Id { get; set; }
        public string ItemId { get; set; } = "";
        public int Quality { get; set; }
        public int Count { get; set; }
    }

    public void Register() => helper.Events.Multiplayer.ModMessageReceived += OnMessage;

    /// <summary>A backend for an online farmhand's Terminal at this tile.</summary>
    public ITerminalBackend RemoteBackend(GameLocation location, Vector2 tile) => new Remote(this, location.NameOrUniqueName, (int)tile.X, (int)tile.Y);

    private void Send(object msg, string type, long to) => helper.Multiplayer.SendMessage(msg, type, new[] { modId }, new[] { to });

    private int Ask(Request req, string type, Action<object> reply)
    {
        req.Id = ++nextRequest;
        pending[req.Id] = reply;
        Send(req, type, Game1.MasterPlayer.UniqueMultiplayerID);
        return req.Id;
    }

    private void OnMessage(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != modId)
            return;

        switch (e.Type)
        {
            // Host side.
            case RefreshType when Context.IsMainPlayer:
            {
                var req = e.ReadAs<Request>();
                var reply = new RowsReply { Id = req.Id };
                if (Storage(req, out var storage, out var loc, out var net))
                {
                    reply.Rows = storage!.Rows().Where(r => r.Key != null)
                        .Select(r => new RowDto { ItemId = r.Key!.Value.QualifiedId, Quality = r.Key.Value.Quality, Count = r.Count }).ToList();
                    reply.Status = LocalTerminalBackend.StatusOf(loc!, net!, storage, power);
                }
                Send(reply, RowsType, e.FromPlayerID);
                break;
            }
            case TakeType when Context.IsMainPlayer:
            {
                var req = e.ReadAs<Request>();
                int given = 0;
                if (Storage(req, out var storage, out _, out _))
                    given = storage!.Take(new ItemKey(req.ItemId, req.Quality), req.Count)?.Stack ?? 0;
                Send(new CountReply { Id = req.Id, ItemId = req.ItemId, Quality = req.Quality, Count = given }, GivenType, e.FromPlayerID);
                break;
            }
            case PutType when Context.IsMainPlayer:
            {
                var req = e.ReadAs<Request>();
                int left = req.Count;
                if (Storage(req, out var storage, out _, out _))
                    left = storage!.Put(StardewItemKeys.Create(new ItemKey(req.ItemId, req.Quality), req.Count))?.Stack ?? 0;
                Send(new CountReply { Id = req.Id, ItemId = req.ItemId, Quality = req.Quality, Count = left }, LeftType, e.FromPlayerID);
                break;
            }

            // Farmhand side.
            case RowsType:
                Resolve(e.ReadAs<RowsReply>().Id, e.ReadAs<RowsReply>());
                break;
            case GivenType:
            case LeftType:
            {
                var reply = e.ReadAs<CountReply>();
                Resolve(reply.Id, reply);
                break;
            }
        }
    }

    private void Resolve(int id, object reply)
    {
        if (pending.Remove(id, out var callback))
            callback(reply);
    }

    private bool Storage(Request req, out NetworkStorage? storage, out LocationNetworks? loc, out StardewEnergistics.Core.Graph.Network? net)
    {
        storage = null;
        loc = null;
        net = null;
        var location = Game1.getLocationFromName(req.Location) ?? Game1.getLocationFromName(req.Location, true);
        if (location == null)
            return false;
        loc = networks.For(location);
        net = loc.NetworkAt(new Vector2(req.X, req.Y));
        if (net == null || !net.Working)
            return false;
        storage = NetworkStorage.For(loc, net, disks);
        return true;
    }

    /// <summary>The farmhand's side of a Terminal.</summary>
    private sealed class Remote : ITerminalBackend
    {
        private readonly TerminalNet net;
        private readonly string location;
        private readonly int x, y;

        public Remote(TerminalNet net, string location, int x, int y)
        {
            this.net = net;
            this.location = location;
            this.x = x;
            this.y = y;
        }

        public bool CanCraft => false;

        private Request Req(string itemId = "", int quality = 0, int count = 0) =>
            new() { Location = location, X = x, Y = y, ItemId = itemId, Quality = quality, Count = count };

        public void Refresh(Action<IReadOnlyList<TerminalRow>, TerminalStatus> done) =>
            net.Ask(Req(), RefreshType, reply =>
            {
                var r = (RowsReply)reply;
                var rows = r.Rows.Select(row => new TerminalRow(new ItemKey(row.ItemId, row.Quality), StardewItemKeys.Create(new ItemKey(row.ItemId, row.Quality), 1), row.Count)).ToList();
                done(rows, r.Status ?? new TerminalStatus(false, 0, 0, 0, 0, 0, 0, 0));
            });

        public void Take(TerminalRow row, int count, Action done)
        {
            if (row.Handle is not ItemKey key)
            {
                done();
                return;
            }
            net.Ask(Req(key.QualifiedId, key.Quality, count), TakeType, reply =>
            {
                var r = (CountReply)reply;
                if (r.Count > 0)
                    TerminalItems.Give(StardewItemKeys.Create(new ItemKey(r.ItemId, r.Quality), r.Count));
                done();
            });
        }

        public void Put(Item item, Action<Item?> done)
        {
            if (!StardewItemKeys.TryGetKey(item, out var key))
            {
                done(item);
                return;
            }
            net.Ask(Req(key.QualifiedId, key.Quality, item.Stack), PutType, reply =>
            {
                var r = (CountReply)reply;
                done(r.Count > 0 ? StardewItemKeys.Create(key, r.Count) : null);
            });
        }
    }
}
