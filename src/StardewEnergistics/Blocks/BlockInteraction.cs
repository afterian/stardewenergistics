using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Graph;
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

    public BlockInteraction(IModHelper helper, NetworkManager networks, DiskStore disks, PowerService power, TerminalController terminal)
    {
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
                terminal.Open(location, tile);
                break;
        }
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
            }
        }
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
