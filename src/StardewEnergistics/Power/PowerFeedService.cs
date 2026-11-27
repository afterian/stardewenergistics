using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;

namespace StardewEnergistics.Power;

/// <summary>
/// Power Feeds (host, every 10 game minutes): a Lightning Rod or Solar Panel with a Power Feed puts its
/// finished Battery Pack straight into a free Power Rack slot of its network (outdoor wire uses the
/// network its Network Port joins). If every slot is full the battery waits in the machine.
/// </summary>
internal sealed class PowerFeedService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly PowerService power;

    public PowerFeedService(IModHelper helper, IMonitor monitor, NetworkManager networks, PowerService power)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.networks = networks;
        this.power = power;
    }

    public void OnTimeChanged()
    {
        if (!Context.IsMainPlayer)
            return;
        int charged = 0;
        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks.Where(loc.IsActive))
            {
                var feeds = loc.ObjectsOf(n, BlockKind.PowerFeed).ToList();
                if (feeds.Count == 0 || networks.Home(loc, n) is not { } home)
                    continue;
                var racks = home.Loc.ObjectsOf(home.Net, BlockKind.PowerRack).ToList();
                bool added = false;
                foreach (var feed in feeds)
                {
                    if (!MachineDriver.IsReady(feed) || feed.heldObject.Value?.QualifiedItemId != "(O)" + VanillaIds.BatteryPack)
                        continue;
                    var rack = racks.FirstOrDefault(r => RackSlots.GetInts(r, Ids.Data.RackCharge).Count < RackSlots.SlotsPerRack);
                    if (rack == null)
                        break;
                    var charge = RackSlots.GetInts(rack, Ids.Data.RackCharge);
                    charge.Add(power.Table.BatteryPower);
                    RackSlots.SetInts(rack, Ids.Data.RackCharge, charge);
                    RackSlots.ShowFrame(rack, charge.Count);
                    MachineDriver.Collect(feed);
                    added = true;
                    charged++;
                }
                if (added)
                    power.Recheck(home.Loc, home.Net);
            }
        }
        if (charged > 0)
        {
            Game1.addHUDMessage(new HUDMessage(helper.Translation.Get("feed.charged", new { count = charged })) { noIcon = true });
            monitor.Log($"Power Feeds charged {charged} Battery Pack(s).", LogLevel.Trace);
        }
    }
}
