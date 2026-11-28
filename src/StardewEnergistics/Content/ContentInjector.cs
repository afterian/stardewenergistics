using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Core.Art;
using StardewEnergistics.Core.Progression;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using static StardewEnergistics.Content.VanillaIds;

namespace StardewEnergistics.Content;

/// <summary>Adds the mod's items, recipes, machine rules and Maru's letters to the game's data assets.</summary>
internal sealed class ContentInjector
{
    private readonly IModHelper helper;

    public ContentInjector(IModHelper helper) => this.helper = helper;

    /// <summary>One item the mod adds. <see cref="Recipe"/> is the ingredient list; yield count follows.</summary>
    private sealed record ItemDef(string Id, string Key, string File, bool Big, int Price, TierId Tier, string Recipe, int Yield = 1, int SpriteIndex = 0);

    private static readonly ItemDef[] Items =
    {
        // T1
        new(Ids.ProcessorPress, "ProcessorPress", "processor-press.png", true, 500, TierId.T1Components, $"{CopperBar} 5 {IronBar} 2 {RefinedQuartz} 1"),
        new(Ids.CopperProcessor, "CopperProcessor", "copper-processor.png", false, 50, TierId.T1Components, ""),
        new(Ids.CopperWire, "CopperWire", "copper-wire-spool.png", false, 5, TierId.T1Components, $"{CopperBar} 1 {Fiber} 1", Yield: 10),
        // T2
        new(Ids.NetworkWrench, "NetworkWrench", "network-wrench.png", false, 100, TierId.T2SimpleStorage, $"{IronBar} 2 {Ids.CopperProcessor} 1"),
        new(Ids.ComputerCore, "ComputerCore", "computer-core.png", true, 1000, TierId.T2SimpleStorage, $"{Ids.CopperProcessor} 4 {IronBar} 5 {BatteryPack} 1"),
        new(Ids.StorageRack, "StorageRack", "storage-rack.png", true, 400, TierId.T2SimpleStorage, $"{Ids.CopperProcessor} 2 {IronBar} 3"),
        new(Ids.PowerRack, "PowerRack", "power-rack.png", true, 300, TierId.T2SimpleStorage, $"{Ids.CopperProcessor} 1 {CopperBar} 2 {BatteryPack} 1"),
        new(Ids.CoalGenerator, "CoalGenerator", "coal-generator.png", true, 300, TierId.T2SimpleStorage, $"{Ids.CopperProcessor} 1 {Big(FurnaceBig)} 1 {Stone} 5"),
        new(Ids.NetworkTerminal, "NetworkTerminal", "network-terminal.png", true, 400, TierId.T2SimpleStorage, $"{Ids.CopperProcessor} 2 {RefinedQuartz} 1 {CopperBar} 1"),
        new(Ids.ChestLink, "ChestLink", "chest-link.png", false, 80, TierId.T2SimpleStorage, $"{Ids.CopperProcessor} 1 {CopperBar} 1"),
        new(Ids.CopperDisk, "CopperDisk", "copper-disk.png", false, 120, TierId.T2SimpleStorage, $"{CopperBar} 2 {Ids.CopperProcessor} 1"),
        // T3
        new(Ids.GoldProcessor, "GoldProcessor", "gold-processor.png", false, 250, TierId.T3Automation, ""),
        new(Ids.HeavyCable, "HeavyCable", "heavy-cable-tiles.png", false, 15, TierId.T3Automation, $"{Ids.CopperWire} 2 {IronBar} 1 {Cloth} 1", Yield: 10, SpriteIndex: 15),
        new(Ids.IronDisk, "IronDisk", "iron-disk.png", false, 400, TierId.T3Automation, $"{Ids.CopperDisk} 1 {IronBar} 3 {Ids.CopperProcessor} 1"),
        new(Ids.GoldDisk, "GoldDisk", "gold-disk.png", false, 1200, TierId.T3Automation, $"{Ids.IronDisk} 1 {GoldBar} 3 {Ids.GoldProcessor} 1"),
        new(Ids.MachineLink, "MachineLink", "machine-link.png", false, 300, TierId.T3Automation, $"{Ids.GoldProcessor} 1 {IronBar} 2"),
        new(Ids.ShippingLink, "ShippingLink", "shipping-link.png", false, 400, TierId.T3Automation, $"{Ids.MachineLink} 1 {GoldBar} 1"),
        // T4
        new(Ids.CraftingRack, "CraftingRack", "crafting-rack.png", true, 1500, TierId.T4Autocrafting, $"{Ids.GoldProcessor} 2 {GoldBar} 2 {Diamond} 1"),
        // T5
        new(Ids.IridiumProcessor, "IridiumProcessor", "iridium-processor.png", false, 1000, TierId.T5LinkedBuildings, ""),
        new(Ids.NetworkPort, "NetworkPort", "network-port.png", true, 2000, TierId.T5LinkedBuildings, $"{Ids.IridiumProcessor} 2 {Ids.CopperWire} 5 {GoldBar} 1"),
        new(Ids.IridiumDisk, "IridiumDisk", "iridium-disk.png", false, 4000, TierId.T5LinkedBuildings, $"{Ids.GoldDisk} 1 {IridiumBar} 3 {Ids.IridiumProcessor} 1"),
        new(Ids.WirelessTerminal, "WirelessTerminal", "wireless-terminal.png", false, 3000, TierId.T5LinkedBuildings, $"{Big(Ids.NetworkTerminal)} 1 {Ids.IridiumProcessor} 1 {BatteryPack} 1"),
        new(Ids.PowerFeed, "PowerFeed", "power-feed.png", false, 800, TierId.T5LinkedBuildings, $"{Ids.GoldProcessor} 1 {IridiumBar} 2"),
        // T6
        new(Ids.AdvancedCraftingRack, "AdvancedCraftingRack", "advanced-crafting-rack.png", true, 5000, TierId.T6WholeFarm, $"{Big(Ids.CraftingRack)} 1 {Ids.IridiumProcessor} 2 {QiGem} 5"),
        new(Ids.PrismaticDisk, "PrismaticDisk", "prismatic-disk.png", false, 10000, TierId.T6WholeFarm, $"{Ids.IridiumDisk} 1 {PrismaticShard} 1 {QiGem} 10"),
        new(Ids.IslandUplink, "IslandUplink", "island-uplink.png", true, 8000, TierId.T6WholeFarm, $"{Ids.IridiumProcessor} 2 {PrismaticShard} 1 {RadioactiveBar} 5"),
    };

    /// <summary>The quest guide journal: not in a tier; Maru mails it on day one and its recipe comes with it.</summary>
    private static readonly ItemDef Journal = new(Ids.QuestJournal, "QuestJournal", "", false, 0, TierId.T1Components, $"{Wood} 1 {Fiber} 1");

    /// <summary>Lost Book sprite in Maps/springobjects, used for the journal until it has its own art.</summary>
    private const int JournalSpriteIndex = 102;

    /// <summary>Qualified item ID for a mod item key such as "CopperDisk" or "ComputerCore".</summary>
    public static string QualifiedIdOf(string key)
    {
        var item = key == Journal.Key ? Journal : Items.First(i => i.Key == key);
        return (item.Big ? "(BC)" : "(O)") + item.Id;
    }

    /// <summary>Mail ID of Maru's day-one letter with the journal.</summary>
    public const string GuideMailId = Ids.ModId + "_Maru_Guide";

    /// <summary>Mail ID of Maru's thank-you letter for finishing a journal chapter.</summary>
    public static string GuideChapterMailId(int chapter) => $"{Ids.ModId}_Maru_GuideCh{chapter}";

    /// <summary>Crafting recipes taught when a tier unlocks.</summary>
    public static IEnumerable<string> RecipesFor(TierId tier) => Items.Where(i => i.Tier == tier && i.Recipe != "").Select(i => i.Id);

    /// <summary>Qualified IDs of every item the mod adds, in tier order (for the test catalog).</summary>
    public static IEnumerable<string> AllQualifiedItemIds => Items.OrderBy(i => i.Tier).Select(i => (i.Big ? "(BC)" : "(O)") + i.Id).Append("(O)" + Ids.QuestJournal);

    public static IEnumerable<string> AllRecipes => Items.Where(i => i.Recipe != "").Select(i => i.Id);

    /// <summary>Mail ID of Maru's letter for a tier.</summary>
    public static string MailId(TierId tier) => $"{Ids.ModId}_Maru_T{(int)tier}";

    /// <summary>Maru's low-power letter (sent again on each new drain).</summary>
    public const string LowPowerMailId = Ids.ModId + "_Maru_PowerLow";

    public static readonly TierId[] TiersWithMail = { TierId.T1Components, TierId.T2SimpleStorage, TierId.T3Automation, TierId.T4Autocrafting, TierId.T5LinkedBuildings, TierId.T6WholeFarm };

    public void Register()
    {
        ProcessorPressOutput.Translation = helper.Translation;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Content.LocaleChanged += (_, _) =>
        {
            foreach (string asset in new[] { "Data/Objects", "Data/BigCraftables", "Data/CraftingRecipes", "Data/Machines", "Data/mail" })
                helper.GameContent.InvalidateCache(asset);
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
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
            e.Edit(asset => AddMail(asset.AsDictionary<string, string>().Data));
    }

    private void AddObjects(IDictionary<string, ObjectData> data)
    {
        foreach (var i in Items.Where(i => !i.Big))
        {
            data[i.Id] = new ObjectData
            {
                Name = i.Id,
                DisplayName = T($"item.{i.Key}.name"),
                Description = T($"item.{i.Key}.description"),
                Type = "Basic", // used on things, never placed (see NotPlaceablePatch)
                Category = 0,
                Price = i.Price,
                Texture = AssetCatalog.Get(i.File).GameAssetName,
                SpriteIndex = i.SpriteIndex,
                Edibility = -300,
                ContextTags = new List<string> { "stardew_energistics_item" },
            };
        }

        data[Journal.Id] = new ObjectData
        {
            Name = Journal.Id,
            DisplayName = T("item.QuestJournal.name"),
            Description = T("item.QuestJournal.description"),
            Type = "Basic",
            Category = 0,
            Price = 0,
            SpriteIndex = JournalSpriteIndex,
            Edibility = -300,
            ContextTags = new List<string> { "stardew_energistics_item", "not_giftable" },
        };
    }

    private void AddBigCraftables(IDictionary<string, BigCraftableData> data)
    {
        foreach (var i in Items.Where(i => i.Big))
        {
            data[i.Id] = new BigCraftableData
            {
                Name = i.Id,
                DisplayName = T($"item.{i.Key}.name"),
                Description = T($"item.{i.Key}.description"),
                Price = i.Price,
                Fragility = 0,
                CanBePlacedIndoors = true,
                CanBePlacedOutdoors = true,
                Texture = AssetCatalog.Get(i.File).GameAssetName,
                SpriteIndex = 0,
            };
        }
    }

    private void AddRecipes(IDictionary<string, string> data)
    {
        // Format (1.6): ingredients / unused / yield / isBigCraftable / unlock / display name
        foreach (var i in Items.Where(i => i.Recipe != ""))
        {
            string yield = i.Yield > 1 ? $"{i.Id} {i.Yield}" : i.Id;
            data[i.Id] = $"{i.Recipe}/Home/{yield}/{(i.Big ? "true" : "false")}/null/{T($"item.{i.Key}.name")}";
        }
        data[Journal.Id] = $"{Journal.Recipe}/Home/{Journal.Id}/false/null/{T("item.QuestJournal.name")}";
    }

    private void AddMail(IDictionary<string, string> data)
    {
        foreach (var tier in TiersWithMail)
        {
            string recipes = string.Join("", RecipesFor(tier).Select(id => $" %item craftingRecipe {id} %%"));
            data[MailId(tier)] = $"{T($"mail.T{(int)tier}.text")}{recipes}[#]{T($"mail.T{(int)tier}.title")}";
        }
        data[LowPowerMailId] = $"{T("mail.powerLow.text")}[#]{T("mail.powerLow.title")}";

        data[GuideMailId] = $"{T("mail.guide.text")} %item object {Ids.QuestJournal} 1 %%[#]{T("mail.guide.title")}";
        foreach (var chapter in Core.Guide.GuideBook.Chapters.Where(c => c.RewardItem != null))
            data[GuideChapterMailId(chapter.Number)] = $"{T($"mail.guideCh{chapter.Number}.text")} %item object {chapter.RewardItem} {chapter.RewardCount} %%[#]{T($"mail.guideCh{chapter.Number}.title")}";
    }

    private void AddMachines(IDictionary<string, MachineData> data)
    {
        data[Big(Ids.ProcessorPress)] = new MachineData
        {
            InvalidItemMessage = T("press.invalid"),
            ShowNextIndexWhileWorking = true,
            OutputRules = new List<MachineOutputRule>
            {
                PressRule("CopperProcessor", CopperBar, Ids.CopperProcessor, days: 1),
                PressRule("GoldProcessor", GoldBar, Ids.GoldProcessor, days: 1),
                PressRule("IridiumProcessor", IridiumBar, Ids.IridiumProcessor, days: 2),
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
                RequiredItemId = Obj(barId),
                RequiredCount = 1,
            },
        },
        OutputItem = new List<MachineItemOutput>
        {
            new()
            {
                Id = outputId,
                ItemId = Obj(outputId),
                OutputMethod = "StardewEnergistics.Content.ProcessorPressOutput, StardewEnergistics: GetOutput",
            },
        },
        DaysUntilReady = days,
    };
}
