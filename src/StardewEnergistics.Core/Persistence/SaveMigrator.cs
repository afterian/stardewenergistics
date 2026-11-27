namespace StardewEnergistics.Core.Persistence;

/// <summary>Upgrades older save models to <see cref="SaveModel.CurrentVersion"/>.</summary>
public static class SaveMigrator
{
    public static SaveModel Migrate(SaveModel model)
    {
        if (model.SchemaVersion > SaveModel.CurrentVersion)
            throw new InvalidOperationException(
                $"Save data is from a newer Stardew Energistics (schema {model.SchemaVersion}); update the mod.");

        // No older schemas yet. Add one step per version here, e.g. if (model.SchemaVersion == 1) { ...; model.SchemaVersion = 2; }
        model.SchemaVersion = SaveModel.CurrentVersion;
        return model;
    }
}
