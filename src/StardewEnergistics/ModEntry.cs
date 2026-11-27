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
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Monitor.Log($"Save loaded for {Game1.player.Name}; Stardew Energistics is running.", LogLevel.Info);
            Game1.addHUDMessage(new HUDMessage("Stardew Energistics is running!", HUDMessage.newQuest_type));
        }
    }
}
