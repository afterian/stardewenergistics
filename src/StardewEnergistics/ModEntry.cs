using HarmonyLib;
using StardewEnergistics.Blocks;
using StardewEnergistics.Content;
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

            var networks = new NetworkManager(wires);
            networks.Register(helper);
            power = new PowerService(helper, Monitor, networks) { Table = config.PowerTable() };
            progression = new ProgressionService(Monitor, networks, power) { GatesEnabled = config.TierGates };
            var terminal = new TerminalController(helper, Monitor, networks, disks, power);
            terminal.Register();

            var renderer = new WireRenderer(wires, textures, Monitor);
            renderer.Patch(harmony);
            DiskStackPatch.Apply(harmony);

            new WireInput(helper, config, wires, renderer).Register();
            new BlockInteraction(helper, networks, disks, power, terminal).Register();
            new DebugCommands(helper, Monitor, wires, networks, disks, power).Register();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.Saving += (_, _) => save.Save();
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => save.Clear();

            Monitor.Log($"Art: {textures.ExportedCount} of {Core.Art.AssetCatalog.All.Count} sprites exported; the rest use placeholders.", LogLevel.Debug);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            save.Load();
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
