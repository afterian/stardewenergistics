using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Storage;
using StardewEnergistics.World;
using StardewModdingAPI;

namespace StardewEnergistics;

/// <summary>Reads and writes the one save-data model (wire layers + disk contents). Host only.</summary>
internal sealed class SaveCoordinator
{
    public const string SaveKey = "save-v1";

    private readonly IModHelper helper;
    private readonly WireLayerStore wires;
    private readonly DiskStore disks;

    public SaveCoordinator(IModHelper helper, WireLayerStore wires, DiskStore disks)
    {
        this.helper = helper;
        this.wires = wires;
        this.disks = disks;
    }

    public void Load()
    {
        // Farmhands get state from the host (multiplayer sync is later M2 work), so they start empty.
        SaveModel? model = Context.IsMainPlayer ? helper.Data.ReadSaveData<SaveModel>(SaveKey) : null;
        if (model != null)
            model = SaveMigrator.Migrate(model);
        wires.Load(model);
        disks.Load(model);
    }

    public void Save()
    {
        if (!Context.IsMainPlayer)
            return;
        var model = new SaveModel();
        wires.WriteTo(model);
        disks.WriteTo(model);
        helper.Data.WriteSaveData(SaveKey, model);
    }

    public void Clear()
    {
        wires.Clear();
        disks.Clear();
    }
}
