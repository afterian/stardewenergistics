using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Wires;
using StardewEnergistics.World;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewEnergistics.Multiplayer;

/// <summary>
/// Keeps the wire layer the same for every player. The host owns it:
/// <list type="bullet">
/// <item>When a farmhand with the mod has loaded the save, it asks the host for every layer.</item>
/// <item>Any tile change is sent as the tile's new state (last write wins): a farmhand sends it to the
/// host, and the host passes it on to everyone else.</item>
/// </list>
/// Split-screen players share the host's memory, so nothing is sent to or from them.
/// </summary>
internal sealed class WireSync
{
    private const string FullType = "WireFull";
    private const string TileType = "WireTile";
    private const string RequestType = "WireRequest";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly WireLayerStore store;
    private readonly string modId;
    private bool applying;

    public WireSync(IModHelper helper, IMonitor monitor, WireLayerStore store, string modId)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.store = store;
        this.modId = modId;
    }

    public sealed class FullMessage
    {
        public Dictionary<string, WireLayerDto> Wires { get; set; } = new();
    }

    public sealed class TileMessage
    {
        public string Location { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        /// <summary>Null when the tile has no wire.</summary>
        public WireTileDto? Tile { get; set; }
    }

    public void Register()
    {
        store.TileChanged += OnTileChanged;
        helper.Events.Multiplayer.ModMessageReceived += OnMessage;
    }

    /// <summary>Remote peers with the mod (not split-screen screens in this process).</summary>
    private long[] RemotePeers(long? except = null) => helper.Multiplayer.GetConnectedPlayers()
        .Where(p => !p.IsSplitScreen && p.GetMod(modId) != null && p.PlayerID != except)
        .Select(p => p.PlayerID)
        .ToArray();

    /// <summary>Called by a remote farmhand after its save has loaded.</summary>
    public void RequestFullSync()
    {
        if (Context.IsMainPlayer || Context.IsSplitScreen)
            return;
        helper.Multiplayer.SendMessage(new object(), RequestType, new[] { modId }, new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    private void OnTileChanged(string location, TilePos pos)
    {
        if (applying || !Context.IsWorldReady || !Context.IsMultiplayer)
            return;
        if (!Context.IsMainPlayer && Context.IsSplitScreen)
            return; // shared memory with the host

        var msg = new TileMessage { Location = location, X = pos.X, Y = pos.Y, Tile = ToDto(store.For(location).Get(pos)) };
        long[] to = Context.IsMainPlayer ? RemotePeers() : new[] { Game1.MasterPlayer.UniqueMultiplayerID };
        if (to.Length > 0)
            helper.Multiplayer.SendMessage(msg, TileType, new[] { modId }, to);
    }

    private void OnMessage(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != modId)
            return;

        if (e.Type == RequestType && Context.IsMainPlayer)
        {
            helper.Multiplayer.SendMessage(new FullMessage { Wires = store.Snapshot() }, FullType, new[] { modId }, new[] { e.FromPlayerID });
            monitor.Log($"Sent the wire layer to {e.FromPlayerID}.", LogLevel.Trace);
        }
        else if (e.Type == FullType && !Context.IsMainPlayer)
        {
            var msg = e.ReadAs<FullMessage>();
            Apply(() => store.Load(msg.Wires));
        }
        else if (e.Type == TileType)
        {
            var msg = e.ReadAs<TileMessage>();
            var pos = new TilePos(msg.X, msg.Y);
            Apply(() =>
            {
                var layer = store.For(msg.Location);
                if (msg.Tile == null)
                    layer.Remove(pos);
                else
                    layer.Restore(pos, FromDto(msg.Tile));
            });

            // The host passes a farmhand's change on to everyone else.
            if (Context.IsMainPlayer)
            {
                long[] others = RemotePeers(except: e.FromPlayerID);
                if (others.Length > 0)
                    helper.Multiplayer.SendMessage(msg, TileType, new[] { modId }, others);
            }
        }
    }

    private void Apply(System.Action action)
    {
        applying = true;
        try
        {
            action();
        }
        finally
        {
            applying = false;
        }
    }

    private static WireTileDto? ToDto(WireTile? t) =>
        t == null ? null : new WireTileDto { Kind = t.Kind.ToString(), Covered = t.Covered, Color = t.Color };

    private static WireTile FromDto(WireTileDto d) =>
        new(System.Enum.TryParse<WireKind>(d.Kind, out var k) ? k : WireKind.Copper, d.Covered, d.Color is >= 0 and < WireTile.ColorCount ? d.Color : 0);
}
