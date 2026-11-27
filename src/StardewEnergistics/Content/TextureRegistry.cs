using System.IO;
using Microsoft.Xna.Framework.Graphics;
using StardewEnergistics.Core.Art;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace StardewEnergistics.Content;

/// <summary>
/// Registers every catalogue file as its own game asset (Mods/afterian.StardewEnergistics/Sprites/&lt;name&gt;).
/// Loads assets/sprites/&lt;file&gt; when it exists, otherwise a generated placeholder of the right size.
/// </summary>
internal sealed class TextureRegistry
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    public TextureRegistry(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public static string SpritesFolder => Path.Combine("assets", "sprites");

    public int ExportedCount { get; private set; }

    public void Register()
    {
        foreach (var spec in AssetCatalog.All)
        {
            if (File.Exists(Path.Combine(helper.DirectoryPath, SpritesFolder, spec.FileName)))
                ExportedCount++;
        }
        helper.Events.Content.AssetRequested += OnAssetRequested;
    }

    /// <summary>The loaded texture for an art file (real or placeholder).</summary>
    public Texture2D Get(string fileName) => helper.GameContent.Load<Texture2D>(AssetCatalog.Get(fileName).GameAssetName);

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.StartsWith(AssetCatalog.AssetRoot + "/"))
            return;

        foreach (var spec in AssetCatalog.All)
        {
            if (!e.NameWithoutLocale.IsEquivalentTo(spec.GameAssetName))
                continue;

            string relative = Path.Combine(SpritesFolder, spec.FileName);
            if (File.Exists(Path.Combine(helper.DirectoryPath, relative)))
            {
                e.LoadFromModFile<Texture2D>(relative, AssetLoadPriority.Exclusive);
            }
            else
            {
                monitor.Log($"Using a placeholder for {spec.FileName}.", LogLevel.Trace);
                e.LoadFrom(() => PlaceholderFactory.Create(spec), AssetLoadPriority.Exclusive);
            }
            return;
        }
    }
}
