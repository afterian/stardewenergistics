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
        // A split-screen farmhand shares the host's memory: nothing to load.
        if (!Context.IsMainPlayer && Context.IsSplitScreen)
            return;

        // A remote farmhand starts empty; the host sends the wire layer when it joins.
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

    /// <summary>Called on return to title. Only the first screen owns the shared state.</summary>
    public void Clear()
    {
        if (Context.ScreenId != 0)
            return;
        wires.Clear();
        disks.Clear();
    }
}
