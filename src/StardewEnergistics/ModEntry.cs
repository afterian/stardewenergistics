using HarmonyLib;
using StardewEnergistics.Blocks;
using StardewEnergistics.Content;
using StardewEnergistics.Crafting;
using StardewEnergistics.Machines;
using StardewEnergistics.Multiplayer;
using StardewEnergistics.Networks;
using StardewEnergistics.Patches;
using StardewEnergistics.Power;
using StardewEnergistics.Progression;
using StardewEnergistics.Storage;
using StardewEnergistics.UI;
using StardewEnergistics.World;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewEnergistics
{
    /// <summary>Mod entry point: wires the services together and hooks game events.</summary>
    public class ModEntry : Mod
    {
        private ModConfig config = new();
        private SaveCoordinator save = null!;
        private PowerService power = null!;
        private ProgressionService progression = null!;
        private WireSync wireSync = null!;

        public override void Entry(IModHelper helper)
        {
            Monitor.Log("Stardew Energistics loaded", LogLevel.Info);
            config = helper.ReadConfig<ModConfig>();
            var harmony = new Harmony(ModManifest.UniqueID);

            var textures = new TextureRegistry(helper, Monitor);
            textures.Register();
            new ContentInjector(helper).Register();

            var wires = new WireLayerStore(Monitor);
            var disks = new DiskStore(Monitor);
            save = new SaveCoordinator(helper, wires, disks);

            wireSync = new WireSync(helper, Monitor, wires, ModManifest.UniqueID);
            wireSync.Register();

            var networks = new NetworkManager(wires);
            networks.Register(helper);
            power = new PowerService(helper, Monitor, networks) { Table = config.ToPowerTable() };
            progression = new ProgressionService(Monitor, networks, power) { GatesEnabled = config.TierGates };
            var terminalNet = new TerminalNet(helper, Monitor, networks, disks, power, ModManifest.UniqueID);
            terminalNet.Register();
            var crafting = new CraftingService(helper, Monitor, networks, disks, power);
            var feeds = new PowerFeedService(helper, Monitor, networks, power);
            helper.Events.GameLoop.DayStarted += (_, _) => crafting.OnDayStarted();
            var terminal = new TerminalController(helper, Monitor, networks, disks, power, textures, terminalNet, crafting);
            terminal.Register();

            var renderer = new WireRenderer(wires, textures, Monitor);
            renderer.Patch(harmony);
            DiskStackPatch.Apply(harmony);
            DiskUpgradePatch.Apply(harmony, disks);

            var links = new MachineLinkService(Monitor, networks, disks, power);
            var shipping = new ShippingLinkService(helper, Monitor, networks, disks, power);
            helper.Events.GameLoop.TimeChanged += (_, _) =>
            {
                links.OnTimeChanged();
                crafting.OnTimeChanged();
                feeds.OnTimeChanged();
            };
            helper.ConsoleCommands.Add("se_perf", "Run all Machine Links now and show how long it took.", (_, _) =>
            {
                if (!Context.IsWorldReady)
                    return;
                links.OnTimeChanged();
                Monitor.Log($"Machine Links: {links.LastMachines} machines in {links.LastMs:0.00} ms.", LogLevel.Info);
            });
            helper.Events.GameLoop.DayEnding += (_, _) =>
            {
                links.OnDayEnding();
                crafting.OnDayEnding();
                shipping.OnDayEnding();
            };

            new WireInput(helper, config, wires, renderer).Register();
            new BlockInteraction(helper, networks, disks, power, terminal, textures).Register();
            new DebugCommands(helper, Monitor, wires, networks, disks, power).Register();

            new Multiplayer.VersionCheck(helper, Monitor, ModManifest).Register();
            helper.Events.GameLoop.GameLaunched += (_, _) => Integrations.ConfigMenu.Register(helper, ModManifest, config, () =>
            {
                power.Table = config.ToPowerTable();
                progression.GatesEnabled = config.TierGates;
            });

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.Saving += (_, _) => save.Save();
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => save.Clear();

            Monitor.Log($"Art: {textures.ExportedCount} of {Core.Art.AssetCatalog.All.Count} sprites exported; the rest use placeholders.", LogLevel.Debug);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            save.Load();
            wireSync.RequestFullSync();
            Monitor.Log($"Save loaded for {Game1.player.Name}; Stardew Energistics is running.", LogLevel.Info);
            Game1.addHUDMessage(new HUDMessage("Stardew Energistics is running!", HUDMessage.newQuest_type));
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            power.OnDayStarted();
            progression.OnDayStarted();
        }
    }
}
