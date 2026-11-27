using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewEnergistics.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Blocks;

/// <summary>
/// Right-click (action button) on network blocks:
/// Storage Rack takes/gives disks, Power Rack takes Battery Packs, Coal Generator takes fuel,
/// Core shows network status, Terminal opens its menu. A Chest Link is attached by using it on a chest
/// and removed with the Network Wrench.
/// </summary>
internal sealed class BlockInteraction
{
    private readonly IModHelper helper;
    private readonly NetworkManager networks;
    private readonly DiskStore disks;
    private readonly PowerService power;
    private readonly TerminalController terminal;
    private readonly TextureRegistry textures;

    public BlockInteraction(IModHelper helper, NetworkManager networks, DiskStore disks, PowerService power, TerminalController terminal, TextureRegistry textures)
    {
        this.textures = textures;
        this.helper = helper;
        this.networks = networks;
        this.disks = disks;
        this.power = power;
        this.terminal = terminal;
    }

    public void Register() => helper.Events.Input.ButtonPressed += OnButtonPressed;

    private string T(string key, object? tokens = null) => helper.Translation.Get(key, tokens);

    private static void Hud(string text) => Game1.addHUDMessage(new HUDMessage(text) { noIcon = true });

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree || !e.Button.IsActionButton() || helper.Input.IsSuppressed(e.Button))
            return;

        var location = Game1.currentLocation;
        var tile = e.Cursor.GrabTile;
        if (!location.objects.TryGetValue(tile, out var obj))
        {
            // Big craftables are two tiles tall; clicking the top half targets the tile below.
            tile = new Vector2(tile.X, tile.Y + 1);
            if (!location.objects.TryGetValue(tile, out obj) || !obj.bigCraftable.Value)
                return;
        }

        var held = Game1.player.ActiveObject;

        // Machine and Shipping Links (checked first: a Mini-Shipping Bin is a chest).
        if (HandleLinks(e.Button, location, tile, obj, held))
            return;

        // Chest Link on / off.
        if (obj is Chest chest)
        {
            if (held?.QualifiedItemId == "(O)" + Ids.ChestLink)
            {
                helper.Input.Suppress(e.Button);
                if (chest.modData.ContainsKey(Ids.Data.ChestLink))
                {
                    Hud(T("chestLink.already"));
                    return;
                }
                chest.modData[Ids.Data.ChestLink] = "1";
                Game1.player.reduceActiveItemByOne();
                networks.Invalidate(location);
                Game1.playSound("hammer");
                Hud(T("chestLink.added"));
            }
            else if (held?.QualifiedItemId == "(O)" + Ids.NetworkWrench && chest.modData.ContainsKey(Ids.Data.ChestLink))
            {
                helper.Input.Suppress(e.Button);
                chest.modData.Remove(Ids.Data.ChestLink);
                Game1.createItemDebris(ItemRegistry.Create("(O)" + Ids.ChestLink), tile * 64 + new Vector2(32, 32), -1, location);
                networks.Invalidate(location);
                Game1.playSound("hammer");
                Hud(T("chestLink.removed"));
            }
            return;
        }

        switch (BlockIds.KindOf(obj))
        {
            case BlockKind.StorageRack:
                helper.Input.Suppress(e.Button);
                StorageRack(obj, held);
                break;
            case BlockKind.PowerRack:
                helper.Input.Suppress(e.Button);
                PowerRack(location, obj, held);
                break;
            case BlockKind.CoalGenerator:
                helper.Input.Suppress(e.Button);
                Generator(location, obj, held);
                break;
            case BlockKind.Core:
                helper.Input.Suppress(e.Button);
                ShowStatus(location, tile);
                break;
            case BlockKind.Terminal:
                helper.Input.Suppress(e.Button);
                power.ShowTerminals(networks.For(location));
                terminal.Open(location, tile);
                break;
            case BlockKind.CraftingRack:
                helper.Input.Suppress(e.Button);
                ShowJobs(location, tile);
                break;
        }
    }

    private const string MiniShippingBin = "(BC)248";

    private static bool IsHeld(SObject? held, string id) => held?.QualifiedItemId == "(O)" + id;

    /// <summary>Attach a Link by using it on a machine or Mini-Shipping Bin; the wrench opens its settings.</summary>
    private bool HandleLinks(SButton button, GameLocation location, Vector2 tile, SObject obj, SObject? held)
    {
        bool hasMachineLink = obj.modData.ContainsKey(Ids.Data.MachineLink);
        bool hasShippingLink = obj.modData.ContainsKey(Ids.Data.ShippingLink);

        if (IsHeld(held, Ids.MachineLink) || IsHeld(held, Ids.ShippingLink))
        {
            bool shipping = IsHeld(held, Ids.ShippingLink);
            bool fits = shipping ? obj.QualifiedItemId == MiniShippingBin : MachineDriver.IsMachine(obj);
            if (!fits)
            {
                // Let other handlers (and the game) deal with clicks on non-targets, but explain on our own blocks.
                if (obj is not Chest && BlockIds.KindOf(obj) == null && !shipping)
                    return false;
                helper.Input.Suppress(button);
                Hud(T(shipping ? "link.notBin" : "link.notMachine"));
                return true;
            }
            helper.Input.Suppress(button);
            if (hasMachineLink || hasShippingLink || obj.modData.ContainsKey(Ids.Data.ChestLink))
            {
                Hud(T("link.already"));
                return true;
            }
            obj.modData[shipping ? Ids.Data.ShippingLink : Ids.Data.MachineLink] = "1";
            obj.modData[Ids.Data.LinkState] = MachineLinkService.NotSetUp.ToString();
            Game1.player.reduceActiveItemByOne();
            networks.Invalidate(location);
            Game1.playSound("hammer");
            Hud(T("link.added"));
            return true;
        }

        if (IsHeld(held, Ids.NetworkWrench) && (hasMachineLink || hasShippingLink))
        {
            helper.Input.Suppress(button);
            OpenLinkSettings(location, tile, obj, hasShippingLink);
            return true;
        }
        return false;
    }

    private void OpenLinkSettings(GameLocation location, Vector2 tile, SObject obj, bool shipping)
    {
        string title;
        if (shipping)
            title = T("link.title.shipping");
        else
        {
            int count = MachineGroupFinder.Group(new TilePos((int)tile.X, (int)tile.Y), p => MachineDriver.MachineIdAt(location, p.X, p.Y)).Count;
            title = T("link.title.machine", new { machine = obj.DisplayName, count });
        }

        Game1.activeClickableMenu = new LinkSettingsMenu(
            helper.Translation, textures.Get, obj, shipping, title, MachineLinkService.SettingsOf(obj),
            onSave: (target, settings) =>
            {
                MachineLinkService.SetSettings(target, settings);
                if (!settings.Configured)
                    target.modData[Ids.Data.LinkState] = MachineLinkService.NotSetUp.ToString();
                else if (target.modData.TryGetValue(Ids.Data.LinkState, out string? st) && st == MachineLinkService.NotSetUp.ToString())
                    target.modData[Ids.Data.LinkState] = MachineLinkService.Idle.ToString();
            },
            onRemove: target =>
            {
                string item = target.modData.ContainsKey(Ids.Data.ShippingLink) ? Ids.ShippingLink : Ids.MachineLink;
                target.modData.Remove(Ids.Data.MachineLink);
                target.modData.Remove(Ids.Data.ShippingLink);
                target.modData.Remove(Ids.Data.LinkSettings);
                target.modData.Remove(Ids.Data.LinkState);
                Game1.createItemDebris(ItemRegistry.Create("(O)" + item), tile * 64 + new Vector2(32, 32), -1, location);
                networks.Invalidate(location);
                Game1.playSound("hammer");
                Hud(T("link.removed"));
            });
    }

    private void StorageRack(SObject rack, SObject? held)
    {
        var slots = RackSlots.Get(rack, Ids.Data.RackDisks);
        if (DiskStore.IsDisk(held))
        {
            if (slots.Count >= RackSlots.SlotsPerRack)
            {
                Hud(T("rack.full"));
                return;
            }
            var contents = disks.ForItem(held!);
            slots.Add($"{contents.Id.Value}:{contents.Tier}");
            RackSlots.Set(rack, Ids.Data.RackDisks, slots);
            RackSlots.ShowFrame(rack, slots.Count);
            Game1.player.reduceActiveItemByOne();
            Game1.playSound("Ship");
            Hud(DescribeDisk(contents, held!.DisplayName));
            networks.Invalidate(Game1.currentLocation);
            return;
        }

        if (slots.Count == 0)
        {
            Hud(T("rack.empty"));
            return;
        }

        // Take out the last disk, with its contents.
        string last = slots[^1];
        string[] parts = last.Split(':');
        var tier = parts.Length > 1 && System.Enum.TryParse<StardewEnergistics.Core.Storage.DiskTier>(parts[1], out var t) ? t : StardewEnergistics.Core.Storage.DiskTier.Copper;
        var disk = disks.CreateItem(disks.Get(parts[0], tier));
        if (!Game1.player.couldInventoryAcceptThisItem(disk))
        {
            Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Crop.cs.588"));
            return;
        }
        slots.RemoveAt(slots.Count - 1);
        RackSlots.Set(rack, Ids.Data.RackDisks, slots);
        RackSlots.ShowFrame(rack, slots.Count);
        Game1.player.addItemToInventoryBool(disk);
        Game1.playSound("pickUpItem");
        networks.Invalidate(Game1.currentLocation);
    }

    private string DescribeDisk(StardewEnergistics.Core.Storage.DiskContents d, string name) => T("disk.contents", new
    {
        name,
        kinds = d.KindsUsed,
        maxKinds = d.Spec.UnlimitedKinds ? "∞" : d.Spec.MaxKinds.ToString(),
        items = d.TotalItems.ToString("N0"),
        maxItems = d.Spec.MaxItems.ToString("N0"),
    });

    private void PowerRack(GameLocation location, SObject rack, SObject? held)
    {
        var charge = RackSlots.GetInts(rack, Ids.Data.RackCharge);
        if (held?.QualifiedItemId == "(O)" + VanillaIds.BatteryPack)
        {
            if (charge.Count >= RackSlots.SlotsPerRack)
            {
                Hud(T("rack.full"));
                return;
            }
            charge.Add(power.Table.BatteryPower);
            RackSlots.SetInts(rack, Ids.Data.RackCharge, charge);
            RackSlots.ShowFrame(rack, charge.Count);
            Game1.player.reduceActiveItemByOne();
            Game1.playSound("Ship");
            ResetPoweredFlag(location, rack.TileLocation);
        }
        Hud(T("power.rack", new { count = charge.Count, charge = charge.Sum() }));
    }

    private void Generator(GameLocation location, SObject gen, SObject? held)
    {
        int fuel = RackSlots.GetInt(gen, Ids.Data.Fuel);
        int per = held?.QualifiedItemId switch
        {
            "(O)" + VanillaIds.Coal => power.Table.CoalPower,
            "(O)" + VanillaIds.Hardwood => power.Table.HardwoodPower,
            _ => 0,
        };
        if (per > 0)
        {
            fuel += per * held!.Stack;
            Game1.player.removeItemFromInventory(held);
            gen.modData[Ids.Data.Fuel] = fuel.ToString();
            RackSlots.ShowFrame(gen, 1);
            Game1.playSound("fireball");
            ResetPoweredFlag(location, gen.TileLocation);
        }
        Hud(T("power.generator", new { fuel }));
    }

    /// <summary>After adding power mid-day, let the network re-check whether it has enough.</summary>
    private void ResetPoweredFlag(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        if (n?.Core != null)
        {
            var core = loc.ObjectOf(n.Core);
            if (core.modData.TryGetValue(Ids.Data.Powered, out string? flag) && flag == "0")
            {
                core.modData.Remove(Ids.Data.Powered);
                RackSlots.ShowFrame(core, power.IsPowered(loc, n) ? 1 : 0);
                power.ShowTerminals(loc);
            }
        }
    }

    private void ShowJobs(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        if (n?.Core == null)
        {
            Hud(T("auto.noRack"));
            return;
        }
        var jobs = StardewEnergistics.Crafting.CraftingService.JobsOf(loc.ObjectOf(n.Core)).Jobs.Where(j => !j.Finished).ToList();
        string list = jobs.Count == 0
            ? T("auto.noJobs")
            : string.Join(", ", jobs.Select(j => $"{j.Target.Count} {StardewEnergistics.Crafting.CraftingService.NameOf(j.Target.Item.QualifiedId)} ({(int)(j.Progress * 100)}%)"));
        Hud(T("rack.crafting", new { jobs = list }));
    }

    private void ShowStatus(GameLocation location, Vector2 tile)
    {
        var loc = networks.For(location);
        var n = loc.NetworkAt(tile);
        if (n == null)
            return;

        var issue = loc.Graph.Issues.FirstOrDefault(i => n.Blocks.Any(b => b.Id == i.BlockId));
        if (issue != null)
        {
            Game1.showRedMessage(T($"network.problem.{issue.Kind}"));
            return;
        }

        var storage = NetworkStorage.For(loc, n, disks);
        Game1.drawObjectDialogue(T("network.status", new
        {
            blocks = n.Blocks.Count,
            disks = storage.Disks.Disks.Count,
            kinds = storage.KindsStored,
            items = storage.Disks.TotalItems.ToString("N0"),
            power = power.Describe(loc, n),
        }));
    }
}
