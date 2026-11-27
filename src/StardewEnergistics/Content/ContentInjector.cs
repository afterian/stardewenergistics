using System.Collections.Generic;
using StardewEnergistics.Core.Art;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace StardewEnergistics.Content;

/// <summary>Adds the mod's items, recipes and machine rules to the game's data assets.</summary>
internal sealed class ContentInjector
{
    private readonly IModHelper helper;

    public ContentInjector(IModHelper helper) => this.helper = helper;

    /// <summary>Recipes the player can be taught (by se_unlock now, by Maru's mail from M2).</summary>
    public static readonly string[] T1Recipes = { Ids.ProcessorPress, Ids.CopperWire };
    public static readonly string[] M1DebugRecipes = { Ids.ProcessorPress, Ids.CopperWire, Ids.HeavyCable, Ids.NetworkWrench };

    public void Register()
    {
        ProcessorPressOutput.Translation = helper.Translation;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Content.LocaleChanged += (_, _) =>
        {
            helper.GameContent.InvalidateCache("Data/Objects");
            helper.GameContent.InvalidateCache("Data/BigCraftables");
            helper.GameContent.InvalidateCache("Data/CraftingRecipes");
            helper.GameContent.InvalidateCache("Data/Machines");
        };
    }

    private string T(string key) => helper.Translation.Get(key).ToString();

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            e.Edit(asset => AddObjects(asset.AsDictionary<string, ObjectData>().Data));
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            e.Edit(asset => AddBigCraftables(asset.AsDictionary<string, BigCraftableData>().Data));
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            e.Edit(asset => AddRecipes(asset.AsDictionary<string, string>().Data));
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Machines"))
            e.Edit(asset => AddMachines(asset.AsDictionary<string, MachineData>().Data));
    }

    private void AddObjects(IDictionary<string, ObjectData> data)
    {
        Obj(data, Ids.CopperProcessor, "CopperProcessor", "copper-processor.png", 50);
        Obj(data, Ids.GoldProcessor, "GoldProcessor", "gold-processor.png", 250);
        Obj(data, Ids.IridiumProcessor, "IridiumProcessor", "iridium-processor.png", 1000);
        Obj(data, Ids.CopperWire, "CopperWire", "copper-wire-spool.png", 5);
        Obj(data, Ids.HeavyCable, "HeavyCable", "heavy-cable-tiles.png", 15, spriteIndex: 15); // inventory icon = the cross frame until it has its own icon
        Obj(data, Ids.NetworkWrench, "NetworkWrench", "network-wrench.png", 100);
    }

    private void Obj(IDictionary<string, ObjectData> data, string id, string key, string file, int price, int spriteIndex = 0)
    {
        data[id] = new ObjectData
        {
            Name = id,
            DisplayName = T($"item.{key}.name"),
            Description = T($"item.{key}.description"),
            Type = "Crafting",
            Category = 0,
            Price = price,
            Texture = AssetCatalog.Get(file).GameAssetName,
            SpriteIndex = spriteIndex,
            Edibility = -300,
            ContextTags = new List<string> { "stardew_energistics_item" },
        };
    }

    private void AddBigCraftables(IDictionary<string, BigCraftableData> data)
    {
        data[Ids.ProcessorPress] = new BigCraftableData
        {
            Name = Ids.ProcessorPress,
            DisplayName = T("item.ProcessorPress.name"),
            Description = T("item.ProcessorPress.description"),
            Price = 500,
            Fragility = 0,
            CanBePlacedIndoors = true,
            CanBePlacedOutdoors = true,
            Texture = AssetCatalog.Get("processor-press.png").GameAssetName,
            SpriteIndex = 0,
        };
    }

    private void AddRecipes(IDictionary<string, string> data)
    {
        // Format (1.6): ingredients / unused / yield / isBigCraftable / unlock / display name
        data[Ids.ProcessorPress] = $"{VanillaIds.CopperBar} 5 {VanillaIds.IronBar} 2 {VanillaIds.RefinedQuartz} 1/Home/{Ids.ProcessorPress}/true/null/{T("item.ProcessorPress.name")}";
        data[Ids.CopperWire] = $"{VanillaIds.CopperBar} 1 {VanillaIds.Fiber} 1/Home/{Ids.CopperWire} 10/false/null/{T("item.CopperWire.name")}";
        data[Ids.HeavyCable] = $"{Ids.CopperWire} 2 {VanillaIds.IronBar} 1 {VanillaIds.Cloth} 1/Home/{Ids.HeavyCable} 10/false/null/{T("item.HeavyCable.name")}";
        data[Ids.NetworkWrench] = $"{VanillaIds.IronBar} 2 {Ids.CopperProcessor} 1/Home/{Ids.NetworkWrench}/false/null/{T("item.NetworkWrench.name")}";
    }

    private void AddMachines(IDictionary<string, MachineData> data)
    {
        data[VanillaIds.Big(Ids.ProcessorPress)] = new MachineData
        {
            InvalidItemMessage = T("press.invalid"),
            ShowNextIndexWhileWorking = true,
            OutputRules = new List<MachineOutputRule>
            {
                PressRule("CopperProcessor", VanillaIds.CopperBar, Ids.CopperProcessor, days: 1),
                PressRule("GoldProcessor", VanillaIds.GoldBar, Ids.GoldProcessor, days: 1),
                PressRule("IridiumProcessor", VanillaIds.IridiumBar, Ids.IridiumProcessor, days: 2),
            },
        };
    }

    private static MachineOutputRule PressRule(string ruleId, string barId, string outputId, int days) => new()
    {
        Id = ruleId,
        Triggers = new List<MachineOutputTriggerRule>
        {
            new()
            {
                Id = ruleId + "_Bar",
                Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                RequiredItemId = VanillaIds.Obj(barId),
                RequiredCount = 1,
            },
        },
        OutputItem = new List<MachineItemOutput>
        {
            new()
            {
                Id = outputId,
                ItemId = VanillaIds.Obj(outputId),
                OutputMethod = "StardewEnergistics.Content.ProcessorPressOutput, StardewEnergistics: GetOutput",
            },
        },
        DaysUntilReady = days,
    };
}
