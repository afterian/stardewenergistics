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
            if (config.Upgrade())
                helper.WriteConfig(config);
            var harmony = new Harmony(ModManifest.UniqueID);

            var textures = new TextureRegistry(helper, Monitor);
            textures.Register();
            new ContentInjector(helper).Register();

            var wires = new WireLayerStore(Monitor);
            var disks = new DiskStore(Monitor);
            save = new SaveCoordinator(helper, wires, disks);

            wireSync = new WireSync(helper, Monitor, wires, ModManifest.UniqueID);
            wireSync.Register();

            var networks = new NetworkManager(wires, config.WireUnderBlockOnly ? Core.Graph.ConnectionRule.OwnTile : Core.Graph.ConnectionRule.OwnTileOrNeighbours);
            networks.Register(helper, ModManifest.UniqueID);
            networks.ReachAllBeforePorts = config.TerminalReachAllBeforePorts;
            power = new PowerService(helper, Monitor, networks) { Table = config.ToPowerTable() };
            progression = new ProgressionService(Monitor, networks, power) { GatesEnabled = config.TierGates };
            var terminalNet = new TerminalNet(helper, Monitor, networks, disks, power, ModManifest.UniqueID);
            terminalNet.Register();
            var crafting = new CraftingService(helper, Monitor, networks, disks, power);
            var feeds = new PowerFeedService(helper, Monitor, networks, power);
            helper.Events.GameLoop.DayStarted += (_, _) => crafting.OnDayStarted();
            // Before the other TimeChanged handlers, so the journal sees short autocraft jobs before they finish.
            var guide = new Guide.GuideService(helper, Monitor, config, networks, power, disks, wires, progression);
            guide.Register();
            var terminal = new TerminalController(helper, Monitor, networks, disks, power, textures, terminalNet, crafting);
            terminal.Register();

            var renderer = new WireRenderer(wires, textures, networks, power, config, Monitor);
            renderer.Patch(harmony);
            new OverlayRenderer(textures, networks, power, disks, renderer, Monitor).Patch(harmony);
            DiskStackPatch.Apply(harmony);
            NotPlaceablePatch.Apply(harmony);
            DiskUpgradePatch.Apply(harmony, disks);
            QiGemCostPatch.Apply(harmony);
            Monitor.Log("Patched crafting so Qi Gem recipes spend the Qi Gem balance.", LogLevel.Trace);

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

            var wireInput = new WireInput(helper, Monitor, config, wires, renderer);
            wireInput.Register();
            var blockInteraction = new BlockInteraction(helper, networks, disks, power, terminal, textures);
            blockInteraction.Register();
            new BlockDrops(helper, Monitor, disks, power).Register();
            new DebugCommands(helper, Monitor, wires, networks, disks, power).Register();
            new Testing.TestMenu(helper, Monitor, config, disks).Register();
            new Testing.SelfTest.SelfTestRunner(helper, Monitor, config, new Testing.SelfTest.SelfTestServices
            {
                Wires = wires,
                Networks = networks,
                Disks = disks,
                Power = power,
                Crafting = crafting,
                Links = links,
                Shipping = shipping,
                Feeds = feeds,
                Guide = guide,
                Progression = progression,
                InputHandlers = new object[] { guide, wireInput, blockInteraction },
            }).Register();

            new Multiplayer.VersionCheck(helper, Monitor, ModManifest).Register();
            helper.Events.GameLoop.GameLaunched += (_, _) => Integrations.ConfigMenu.Register(helper, ModManifest, config, () =>
            {
                power.Table = config.ToPowerTable();
                progression.GatesEnabled = config.TierGates;
                networks.ReachAllBeforePorts = config.TerminalReachAllBeforePorts;
                networks.Clear();
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
