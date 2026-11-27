using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewEnergistics.Multiplayer;

/// <summary>Warns when a player joins with a different version of the mod, or without it.</summary>
internal sealed class VersionCheck
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly IManifest manifest;

    public VersionCheck(IModHelper helper, IMonitor monitor, IManifest manifest)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.manifest = manifest;
    }

    public void Register() => helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (e.Peer.IsSplitScreen || !e.Peer.HasSmapi)
            return;
        string name = Game1.GetPlayer(e.Peer.PlayerID)?.Name ?? e.Peer.PlayerID.ToString();
        var mod = e.Peer.GetMod(manifest.UniqueID);
        string? text = null;
        if (mod == null)
            text = helper.Translation.Get("mp.missing", new { name }).ToString();
        else if (!mod.Version.Equals(manifest.Version))
            text = helper.Translation.Get("mp.versionMismatch", new { name, theirs = mod.Version.ToString(), ours = manifest.Version.ToString() }).ToString();
        if (text == null)
            return;
        monitor.Log(text, LogLevel.Warn);
        Game1.addHUDMessage(new HUDMessage(text, HUDMessage.error_type));
    }
}
