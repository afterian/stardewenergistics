using System.Linq;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewValley;

namespace StardewEnergistics.Machines;

/// <summary>
/// Shipping Links (host, each night): for every item on the Link's list, ship everything above its
/// keep-back amount ("ship all Starfruit Wine above 50") into the farm's shipping bin.
/// </summary>
internal sealed class ShippingLinkService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;

    public ShippingLinkService(IModHelper helper, IMonitor monitor, NetworkManager networks, DiskStore disks, PowerService power)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
    }

    public void OnDayEnding()
    {
        if (!Context.IsMainPlayer)
            return;

        var bin = Game1.getFarm().getShippingBin(Game1.MasterPlayer);
        int shipped = 0;
        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks.Where(n => n.Working && power.IsPowered(loc, n)))
            {
                foreach (var link in loc.ObjectsOf(n, BlockKind.ShippingLink))
                {
                    var settings = MachineLinkService.SettingsOf(link);
                    if (!settings.Configured)
                        continue;
                    var storage = NetworkStorage.For(loc, n, disks);
                    foreach (var rule in settings.Inputs)
                    {
                        var keys = rule.AnyQuality
                            ? storage.All().Where(a => a.Key.QualifiedId == rule.Item.QualifiedId).Select(a => a.Key).ToList()
                            : new() { rule.Item };
                        foreach (var key in keys)
                        {
                            long usable = InputSelector.Usable(settings, storage, key);
                            while (usable > 0)
                            {
                                var item = storage.Take(key, (int)System.Math.Min(999, usable));
                                if (item == null)
                                    break;
                                usable -= item.Stack;
                                shipped += item.Stack;
                                bin.Add(item);
                            }
                        }
                    }
                    link.modData[Ids.Data.LinkState] = "1";
                }
            }
        }

        if (shipped > 0)
        {
            monitor.Log($"Shipping Links shipped {shipped} items.", LogLevel.Debug);
            Game1.addHUDMessage(new HUDMessage(helper.Translation.Get("link.shipped", new { count = shipped })) { noIcon = true });
        }
    }
}
