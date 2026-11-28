using StardewEnergistics.Core.Progression;

namespace StardewEnergistics.Core.Guide;

/// <summary>Things that happened somewhere on the farm, worked out by the host from its networks.</summary>
public enum WorldFlag
{
    CorePlaced,
    DiskInRack,
    NetworkPowered,
    GeneratorFueled,
    ChestLinked,
    WireHidden,
    TenKindsStored,
    MachineLinkSetUp,
    ShippingLinked,
    CraftingRackPowered,
    FurnaceLinked,
    JobStarted,
    PortJoined,
    ThreeBuildingsJoined,
    PowerFeedAttached,
    AdvancedRackPowered,
    KeepInStockRule,
    IslandLinked,
}

public enum GoalKind
{
    /// <summary>Open the journal once.</summary>
    OpenJournal,
    MaruHearts,
    CopperProcessors,
    /// <summary>Read Maru's letter for <see cref="GuideGoal.Tier"/>.</summary>
    ReadLetter,
    /// <summary>Craft the item, or have it in your backpack (covers pressed processors and upgraded disks).</summary>
    Get,
    World,
    /// <summary>Every other goal in the journal is done.</summary>
    AllDone,
}

/// <summary>What finishes a goal.</summary>
public sealed record GuideGoal(GoalKind Kind, int Amount = 0, string? Item = null, TierId? Tier = null, WorldFlag? Flag = null)
{
    public static GuideGoal Open() => new(GoalKind.OpenJournal);
    public static GuideGoal Hearts(int n) => new(GoalKind.MaruHearts, Amount: n);
    public static GuideGoal Processors(int n) => new(GoalKind.CopperProcessors, Amount: n);
    public static GuideGoal Letter(TierId t) => new(GoalKind.ReadLetter, Tier: t);
    public static GuideGoal Get(string itemKey) => new(GoalKind.Get, Item: itemKey);
    public static GuideGoal On(WorldFlag f) => new(GoalKind.World, Flag: f);
    public static GuideGoal Everything() => new(GoalKind.AllDone);
}

/// <summary>
/// One journal goal. Text is in i18n as guide.q.&lt;Id&gt;.title / .text.
/// <see cref="Items"/> are mod item keys (e.g. "CopperDisk") whose recipes the journal shows.
/// </summary>
public sealed record GuideQuest(string Id, int Chapter, GuideGoal Goal, IReadOnlyList<string> Items);

/// <summary>
/// A chapter of the journal: one per tier, plus a prologue (0). Text is guide.ch.&lt;Number&gt;.title / .intro.
/// Finishing every goal in a chapter has Maru mail <see cref="RewardCount"/> of vanilla object <see cref="RewardItem"/>.
/// </summary>
public sealed record GuideChapter(int Number, TierId? Tier, IReadOnlyList<GuideQuest> Quests, string? RewardItem, int RewardCount);

/// <summary>The whole quest guide, in the order a player meets it.</summary>
public static class GuideBook
{
    /// <summary>Item key of the journal itself.</summary>
    public const string JournalKey = "QuestJournal";

    public static readonly IReadOnlyList<GuideChapter> Chapters = Build();

    public static IEnumerable<GuideQuest> AllQuests => Chapters.SelectMany(c => c.Quests);

    public static GuideQuest? Find(string id) => AllQuests.FirstOrDefault(q => q.Id == id);

    private static IReadOnlyList<GuideChapter> Build()
    {
        var chapters = new List<GuideChapter>();
        int ch = 0;
        List<GuideQuest> quests = new();

        void Q(string id, GuideGoal goal, params string[] items) => quests.Add(new GuideQuest(id, ch, goal, items));
        void Chapter(TierId? tier, string? reward, int count)
        {
            chapters.Add(new GuideChapter(ch, tier, quests, reward, count));
            quests = new List<GuideQuest>();
            ch++;
        }

        // 0 Prologue: meet Maru.
        Q("open", GuideGoal.Open(), JournalKey);
        Q("maru", GuideGoal.Hearts(2));
        Chapter(null, null, 0);

        // 1 Components.
        Q("letter1", GuideGoal.Letter(TierId.T1Components));
        Q("press", GuideGoal.Get("ProcessorPress"), "ProcessorPress");
        Q("chip", GuideGoal.Processors(1), "CopperProcessor");
        Q("wire", GuideGoal.Get("CopperWire"), "CopperWire");
        Q("tenChips", GuideGoal.Processors(10), "CopperProcessor");
        Chapter(TierId.T1Components, "338", 5); // Refined Quartz

        // 2 First network.
        Q("letter2", GuideGoal.Letter(TierId.T2SimpleStorage));
        Q("core", GuideGoal.On(WorldFlag.CorePlaced), "ComputerCore");
        Q("disk", GuideGoal.On(WorldFlag.DiskInRack), "StorageRack", "CopperDisk");
        Q("power", GuideGoal.On(WorldFlag.NetworkPowered), "PowerRack");
        Q("generator", GuideGoal.On(WorldFlag.GeneratorFueled), "CoalGenerator");
        Q("terminal", GuideGoal.On(WorldFlag.TenKindsStored), "NetworkTerminal");
        Q("chest", GuideGoal.On(WorldFlag.ChestLinked), "ChestLink");
        Q("wrench", GuideGoal.On(WorldFlag.WireHidden), "NetworkWrench");
        Chapter(TierId.T2SimpleStorage, "787", 2); // Battery Pack

        // 3 Automation.
        Q("letter3", GuideGoal.Letter(TierId.T3Automation));
        Q("goldChip", GuideGoal.Get("GoldProcessor"), "GoldProcessor");
        Q("machineLink", GuideGoal.On(WorldFlag.MachineLinkSetUp), "MachineLink");
        Q("shipping", GuideGoal.On(WorldFlag.ShippingLinked), "ShippingLink");
        Q("cable", GuideGoal.Get("HeavyCable"), "HeavyCable");
        Q("ironDisk", GuideGoal.Get("IronDisk"), "IronDisk");
        Q("goldDisk", GuideGoal.Get("GoldDisk"), "GoldDisk");
        Chapter(TierId.T3Automation, "336", 3); // Gold Bar

        // 4 Autocrafting.
        Q("letter4", GuideGoal.Letter(TierId.T4Autocrafting));
        Q("craftingRack", GuideGoal.On(WorldFlag.CraftingRackPowered), "CraftingRack");
        Q("furnace", GuideGoal.On(WorldFlag.FurnaceLinked), "MachineLink");
        Q("job", GuideGoal.On(WorldFlag.JobStarted));
        Chapter(TierId.T4Autocrafting, "72", 1); // Diamond

        // 5 Linked buildings.
        Q("letter5", GuideGoal.Letter(TierId.T5LinkedBuildings));
        Q("iridiumChip", GuideGoal.Get("IridiumProcessor"), "IridiumProcessor");
        Q("port", GuideGoal.On(WorldFlag.PortJoined), "NetworkPort", "HeavyCable");
        Q("threeBuildings", GuideGoal.On(WorldFlag.ThreeBuildingsJoined), "NetworkPort");
        Q("wireless", GuideGoal.Get("WirelessTerminal"), "WirelessTerminal");
        Q("feed", GuideGoal.On(WorldFlag.PowerFeedAttached), "PowerFeed");
        Q("iridiumDisk", GuideGoal.Get("IridiumDisk"), "IridiumDisk");
        Chapter(TierId.T5LinkedBuildings, "337", 3); // Iridium Bar

        // 6 Whole farm.
        Q("letter6", GuideGoal.Letter(TierId.T6WholeFarm));
        Q("advancedRack", GuideGoal.On(WorldFlag.AdvancedRackPowered), "AdvancedCraftingRack");
        Q("keepStock", GuideGoal.On(WorldFlag.KeepInStockRule), "AdvancedCraftingRack");
        Q("prismaticDisk", GuideGoal.Get("PrismaticDisk"), "PrismaticDisk");
        Q("uplink", GuideGoal.On(WorldFlag.IslandLinked), "IslandUplink");
        Q("finale", GuideGoal.Everything());
        Chapter(TierId.T6WholeFarm, "74", 1); // Prismatic Shard

        return chapters;
    }
}
