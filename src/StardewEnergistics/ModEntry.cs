using System.IO;
using System.Linq;
using StardewEnergistics.Core.Art;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewEnergistics
{
    /// <summary>Mod entry point. For now it only proves the mod builds and loads.</summary>
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            Monitor.Log("Stardew Energistics loaded", LogLevel.Info);
            LogArtStatus(helper);
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Monitor.Log($"Save loaded for {Game1.player.Name}; Stardew Energistics is running.", LogLevel.Info);
            Game1.addHUDMessage(new HUDMessage("Stardew Energistics is running!", HUDMessage.newQuest_type));
        }

        /// <summary>Also proves StardewEnergistics.Core.dll is bundled and loads in the game.</summary>
        private void LogArtStatus(IModHelper helper)
        {
            string spritesDir = Path.Combine(helper.DirectoryPath, "assets", "sprites");
            int found = AssetCatalog.All.Count(a => File.Exists(Path.Combine(spritesDir, a.FileName)));
            Monitor.Log($"Art: {found} of {AssetCatalog.All.Count} sprites exported; the rest use placeholders.", LogLevel.Debug);
        }
    }
}
