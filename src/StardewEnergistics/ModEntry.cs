using HarmonyLib;
using StardewEnergistics.Content;
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
        private TextureRegistry textures = null!;
        private WireLayerStore wires = null!;

        public override void Entry(IModHelper helper)
        {
            Monitor.Log("Stardew Energistics loaded", LogLevel.Info);
            config = helper.ReadConfig<ModConfig>();

            textures = new TextureRegistry(helper, Monitor);
            textures.Register();
            new ContentInjector(helper).Register();

            wires = new WireLayerStore(helper, Monitor);
            var renderer = new WireRenderer(wires, textures, Monitor);
            renderer.Patch(new Harmony(ModManifest.UniqueID));
            new WireInput(helper, config, wires, renderer).Register();
            new DebugCommands(helper, Monitor, wires).Register();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += (_, _) => wires.Save();
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => wires.Clear();

            Monitor.Log($"Art: {textures.ExportedCount} of {Core.Art.AssetCatalog.All.Count} sprites exported; the rest use placeholders.", LogLevel.Debug);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            wires.Load();
            Monitor.Log($"Save loaded for {Game1.player.Name}; Stardew Energistics is running.", LogLevel.Info);
            Game1.addHUDMessage(new HUDMessage("Stardew Energistics is running!", HUDMessage.newQuest_type));
        }
    }
}
