using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Guide;
using StardewEnergistics.Core.Progression;
using StardewEnergistics.Crafting;
using StardewEnergistics.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Progression;
using StardewEnergistics.Storage;
using StardewEnergistics.UI;
using StardewEnergistics.World;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;

namespace StardewEnergistics.Guide;

/// <summary>
/// The quest guide: Maru mails the Tinkerer's Journal on day one; holding it and pressing the action
/// button (or the journal key) opens <see cref="GuideMenu"/>. Goals tick off per player (saved in the
/// player's modData). Facts about networks are worked out by the host and shared through the farm's modData.
/// </summary>
internal sealed class GuideService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly NetworkManager networks;
    private readonly PowerService power;
    private readonly DiskStore disks;
    private readonly WireLayerStore wires;
    private readonly ProgressionService progression;

    /// <summary>Set after a menu that can change networks closes; the host re-checks on the next tick.</summary>
    private readonly PerScreen<bool> refreshSoon = new();

    public GuideService(IModHelper helper, IMonitor monitor, ModConfig config, NetworkManager networks, PowerService power,
        DiskStore disks, WireLayerStore wires, ProgressionService progression)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;
        this.networks = networks;
        this.power = power;
        this.disks = disks;
        this.wires = wires;
        this.progression = progression;
    }

    /// <summary>Register before other services' TimeChanged handlers, so short autocraft jobs are seen before they finish.</summary>
    public void Register()
    {
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.ConsoleCommands.Add("se_guide", "Quest guide journal. 'se_guide' opens it, 'se_guide reset' clears your ticks, 'se_guide give' gives you the journal.", OnCommand);
        helper.Events.GameLoop.DayStarted += (_, _) => OnDayStarted();
        helper.Events.GameLoop.TimeChanged += (_, _) => Refresh();
        helper.Events.Player.InventoryChanged += (_, e) =>
        {
            if (e.IsLocalPlayer)
                UpdatePlayer();
        };
        helper.Events.Display.MenuChanged += (_, e) =>
        {
            if (e.OldMenu is TerminalMenu or AutocraftMenu or LinkSettingsMenu)
                refreshSoon.Value = true;
        };
        helper.Events.GameLoop.UpdateTicked += (_, _) =>
        {
            if (refreshSoon.Value && Context.IsWorldReady && Game1.activeClickableMenu == null)
            {
                refreshSoon.Value = false;
                Refresh();
            }
        };
    }

    private string T(string key, object? tokens = null) => helper.Translation.Get(key, tokens);

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree)
            return;
        bool key = config.GuideKey.JustPressed();
        bool book = (e.Button.IsActionButton() || e.Button.IsUseToolButton())
            && Game1.player.ActiveObject?.QualifiedItemId == "(O)" + Ids.QuestJournal;
        if (!key && !book)
            return;
        if (key)
            helper.Input.SuppressActiveKeybinds(config.GuideKey);
        else
            helper.Input.Suppress(e.Button);
        Open();
    }

    private void OnCommand(string name, string[] args)
    {
        if (!Context.IsWorldReady)
            return;
        switch (args.FirstOrDefault())
        {
            case "reset":
                Game1.player.modData.Remove(Ids.Data.GuideDone);
                if (Context.IsMainPlayer)
                    Game1.getFarm().modData.Remove(Ids.Data.GuideWorld);
                monitor.Log("Journal ticks cleared. They come back as the journal re-checks.", LogLevel.Info);
                break;
            case "give":
                Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create("(O)" + Ids.QuestJournal));
                break;
            default:
                Open();
                break;
        }
    }

    public void Open()
    {
        if (!Context.IsWorldReady)
            return;
        var done = UpdatePlayer(opened: true);
        Game1.playSound("bigSelect");
        Game1.activeClickableMenu = new GuideMenu(helper.Translation, Facts(Game1.player, opened: true), done, config.TierGates, TextTokens());
    }

    /// <summary>Values the journal text fills in, so it matches this player's config.</summary>
    private object TextTokens() => new
    {
        battery = config.BatteryPower,
        coal = config.CoalPower,
        hardwood = config.HardwoodPower,
    };

    private void OnDayStarted()
    {
        var player = Game1.player;
        if (!player.hasOrWillReceiveMail(ContentInjector.GuideMailId))
            player.mailbox.Add(ContentInjector.GuideMailId);
        if (!player.craftingRecipes.ContainsKey(Ids.QuestJournal))
            player.craftingRecipes.Add(Ids.QuestJournal, 0);
        Refresh();
    }

    private void Refresh()
    {
        if (!Context.IsWorldReady)
            return;
        if (Context.IsMainPlayer)
            UpdateWorld();
        UpdatePlayer();
    }

    // ---- Per player ----

    private GuideFacts Facts(Farmer player, bool opened)
    {
        var letters = ContentInjector.TiersWithMail.Where(t => player.mailReceived.Contains(ContentInjector.MailId(t))).ToHashSet();
        var got = new HashSet<string>();
        foreach (string key in GuideBook.AllQuests.Where(q => q.Goal.Kind == GoalKind.Get).Select(q => q.Goal.Item!).Distinct())
        {
            string qualified = ContentInjector.QualifiedIdOf(key);
            bool crafted = player.craftingRecipes.TryGetValue(Ids.Prefix + key, out int times) && times > 0;
            if (crafted || player.Items.ContainsId(qualified))
                got.Add(key);
        }
        return new GuideFacts(progression.Snapshot(player), opened, letters, got, WorldFlags());
    }

    /// <summary>Ticks off what this player has done; tells them and queues Maru's thank-you letters.</summary>
    private HashSet<string> UpdatePlayer(bool opened = false)
    {
        var player = Game1.player;
        bool fresh = !player.modData.TryGetValue(Ids.Data.GuideDone, out string? raw);
        var before = GuideProgress.Decode(raw);
        if (before.Contains("open"))
            opened = true;
        var now = GuideProgress.Update(Facts(player, opened), before);
        if (now.SetEquals(before) && !fresh)
            return now;

        player.modData[Ids.Data.GuideDone] = GuideProgress.Encode(now);
        var newly = GuideBook.AllQuests.Where(q => now.Contains(q.Id) && !before.Contains(q.Id)).ToList();
        if (newly.Count == 0)
            return now;

        if (fresh && newly.Count > 2)
        {
            Game1.addHUDMessage(new HUDMessage(T("guide.hud.caughtUp", new { count = newly.Count }), HUDMessage.achievement_type));
        }
        else
        {
            foreach (var q in newly.Where(q => q.Id != "open").Take(3))
                Game1.addHUDMessage(new HUDMessage(T("guide.hud.done", new { goal = T($"guide.q.{q.Id}.title") }), HUDMessage.achievement_type));
            int more = newly.Count(q => q.Id != "open") - 3;
            if (more > 0)
                Game1.addHUDMessage(new HUDMessage(T("guide.hud.more", new { count = more }), HUDMessage.achievement_type));
            if (newly.Any(q => q.Id != "open"))
                Game1.playSound("questcomplete");
        }

        foreach (var chapter in GuideProgress.NewlyFinishedChapters(before, now).Where(c => c.RewardItem != null))
        {
            string mail = ContentInjector.GuideChapterMailId(chapter.Number);
            if (player.hasOrWillReceiveMail(mail))
                continue;
            player.mailForTomorrow.Add(mail);
            Game1.addHUDMessage(new HUDMessage(T("guide.hud.chapter"), HUDMessage.newQuest_type));
            monitor.Log($"{player.Name} finished journal chapter {chapter.Number}.", LogLevel.Trace);
        }
        return now;
    }

    // ---- Farm-wide facts (host) ----

    private static HashSet<WorldFlag> WorldFlags()
    {
        var farm = Game1.getFarm();
        var set = new HashSet<WorldFlag>();
        if (farm.modData.TryGetValue(Ids.Data.GuideWorld, out string? raw))
        {
            foreach (string s in raw.Split(','))
                if (System.Enum.TryParse<WorldFlag>(s, out var f))
                    set.Add(f);
        }
        return set;
    }

    private void UpdateWorld()
    {
        var known = WorldFlags();
        HashSet<WorldFlag> seen;
        try
        {
            seen = ScanWorld();
        }
        catch (System.Exception ex)
        {
            monitor.Log($"Journal: couldn't check networks: {ex.Message}", LogLevel.Trace);
            return;
        }
        if (seen.IsSubsetOf(known))
            return;
        known.UnionWith(seen);
        Game1.getFarm().modData[Ids.Data.GuideWorld] = string.Join(",", known.OrderBy(f => f).Select(f => f.ToString()));
    }

    private HashSet<WorldFlag> ScanWorld()
    {
        var flags = new HashSet<WorldFlag>();
        var uplinkLocations = new HashSet<string>();
        bool uplinkOnIsland = false;

        foreach (var loc in networks.AllWithCores())
        {
            foreach (var n in loc.Graph.Networks)
            {
                bool active = loc.IsActive(n);
                bool powered = active && power.IsPowered(loc, n);
                IEnumerable<StardewValley.Object> Objects(BlockKind kind) =>
                    n.OfKind(kind).Select(b => loc.Objects.TryGetValue(b.Id, out var o) ? o : null).OfType<StardewValley.Object>();

                if (n.Working)
                    flags.Add(WorldFlag.CorePlaced);
                if (powered && n.Working)
                    flags.Add(WorldFlag.NetworkPowered);
                if (Objects(BlockKind.StorageRack).Any(r => RackSlots.Get(r, Ids.Data.RackDisks).Count > 0))
                    flags.Add(WorldFlag.DiskInRack);
                if (Objects(BlockKind.CoalGenerator).Any(g => RackSlots.GetInt(g, Ids.Data.Fuel) > 0))
                    flags.Add(WorldFlag.GeneratorFueled);
                if (active && n.OfKind(BlockKind.ChestLink).Any())
                    flags.Add(WorldFlag.ChestLinked);
                if (active && n.OfKind(BlockKind.ShippingLink).Any())
                    flags.Add(WorldFlag.ShippingLinked);
                if (active && n.OfKind(BlockKind.PowerFeed).Any())
                    flags.Add(WorldFlag.PowerFeedAttached);
                if (active && Objects(BlockKind.MachineLink).Any(o => o.QualifiedItemId == "(BC)" + VanillaIds.FurnaceBig))
                    flags.Add(WorldFlag.FurnaceLinked);
                if (powered && Objects(BlockKind.MachineLink).Any(o => MachineLinkService.SettingsOf(o).Configured))
                    flags.Add(WorldFlag.MachineLinkSetUp);
                if (powered && n.OfKind(BlockKind.CraftingRack).Any())
                    flags.Add(WorldFlag.CraftingRackPowered);
                if (powered && n.OfKind(BlockKind.AdvCraftingRack).Any())
                {
                    flags.Add(WorldFlag.AdvancedRackPowered);
                    if (CraftingService.StockRulesOf(loc, n).Count > 0)
                        flags.Add(WorldFlag.KeepInStockRule);
                }
                if (n.Core != null && loc.Objects.TryGetValue(n.Core.Id, out var core) && CraftingService.JobsOf(core).Jobs.Count > 0)
                    flags.Add(WorldFlag.JobStarted);
                if (n.Working && n.OfKind(BlockKind.Terminal).Any() && NetworkStorage.For(loc, n, disks).KindsStored >= 10)
                    flags.Add(WorldFlag.TenKindsStored);
                if (n.OfKind(BlockKind.Port).Any() && networks.GroupOf(n) is { Members.Count: > 1, Working: true })
                    flags.Add(WorldFlag.PortJoined);
                if (n.OfKind(BlockKind.Uplink).Any())
                {
                    uplinkLocations.Add(loc.Location.NameOrUniqueName);
                    uplinkOnIsland |= loc.Location is IslandLocation;
                }
                if (n.Working && loc.Location.NameOrUniqueName == "FarmHouse")
                {
                    int buildings = networks.Members(loc, n).Where(m => m.Net.Working).Select(m => m.Loc.Location.NameOrUniqueName).Distinct().Count();
                    if (buildings >= 3)
                        flags.Add(WorldFlag.ThreeBuildingsJoined);
                }
            }
        }

        if (uplinkOnIsland && uplinkLocations.Count >= 2)
            flags.Add(WorldFlag.IslandLinked);
        if (wires.All.Any(l => l.Value.Tiles.Any(t => t.Value.Covered)))
            flags.Add(WorldFlag.WireHidden);
        return flags;
    }
}
