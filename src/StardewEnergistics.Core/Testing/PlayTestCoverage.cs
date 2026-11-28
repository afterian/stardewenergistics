namespace StardewEnergistics.Core.Testing;

/// <summary>One thing a player can do or get, and the in-game play-test scenarios that exercise it.</summary>
public sealed record PlayTestFeature(string Id, string What, IReadOnlyList<string> Scenarios);

/// <summary>
/// The play-tester's coverage list: every mod item, journal goal, Maru letter and player-facing feature, mapped to
/// the in-game scenarios (src/StardewEnergistics/Testing/SelfTest) that play it through. Like unit tests for code,
/// every feature ships with a simulated play test: `dotnet test` fails if an item in Ids.cs, a journal goal or a
/// tier letter has no entry here, and the in-game `coverage` scenario fails if a scenario named here doesn't exist.
/// <para>Ids: <c>item:&lt;Key&gt;</c> (as in Ids.cs), <c>quest:&lt;id&gt;</c> (GuideBook), <c>letter:T&lt;n&gt;</c>, and feature names.</para>
/// </summary>
public static class PlayTestCoverage
{
    /// <summary>Every scenario id the mod's self-test has (kept in step with Scenarios.All by the `coverage` scenario).</summary>
    public static readonly IReadOnlyList<string> ScenarioIds = new[]
    {
        "coverage", "placement", "wire", "wire-placement", "network", "disks", "terminal", "capacity", "targeting", "break-rack", "chest-link",
        "power", "power-day", "machine-link", "autocraft", "tools", "whole-items", "press", "heavy-cable", "disk-upgrade", "shipping-link",
        "power-feed", "wireless", "keep-stock", "save", "quest", "playthrough", "showcase",
    };

    private static PlayTestFeature F(string id, string what, params string[] scenarios) => new(id, what, scenarios);

    private static IReadOnlyList<PlayTestFeature>? features;

    public static IReadOnlyList<PlayTestFeature> Features => features ??= Build();

    private static IReadOnlyList<PlayTestFeature> Build() => Listed.Concat(
        // Every journal goal: the playthrough checks each chapter's goals tick as it plays them.
        Guide.GuideBook.AllQuests.Select(q => F("quest:" + q.Id, $"journal goal '{q.Id}' (chapter {q.Chapter})", "playthrough"))).ToList();

    private static readonly PlayTestFeature[] Listed =
    {
        // Items (every one is crafted at the crafting menu, or pressed, in the playthrough).
        F("item:ProcessorPress", "Processor Press: place, feed a bar, collect", "press", "playthrough"),
        F("item:CopperProcessor", "Copper Processor pressed", "press", "playthrough"),
        F("item:GoldProcessor", "Gold Processor pressed", "press", "playthrough"),
        F("item:IridiumProcessor", "Iridium Processor pressed", "press", "playthrough"),
        F("item:CopperWire", "Copper Wire crafted and laid", "wire", "playthrough"),
        F("item:HeavyCable", "Heavy Cable crafted, laid, coloured, picked up", "heavy-cable", "playthrough"),
        F("item:NetworkWrench", "Wrench: hide wire, open Link settings, take attachments off", "chest-link", "heavy-cable", "playthrough"),
        F("item:ComputerCore", "Computer Core runs a network; Core status", "network", "playthrough"),
        F("item:StorageRack", "Storage Rack holds disks", "disks", "playthrough"),
        F("item:PowerRack", "Power Rack holds Battery Packs", "power", "playthrough"),
        F("item:CoalGenerator", "Coal Generator burns fuel", "power", "playthrough"),
        F("item:NetworkTerminal", "Terminal screen stores and withdraws", "terminal", "playthrough"),
        F("item:ChestLink", "Chest Link puts a chest on the network", "chest-link", "playthrough"),
        F("item:CopperDisk", "Copper Disk storage and limits", "disks", "capacity", "playthrough"),
        F("item:IronDisk", "Iron Disk crafted from a Copper Disk", "disk-upgrade", "playthrough"),
        F("item:GoldDisk", "Gold Disk crafted from an Iron Disk", "playthrough"),
        F("item:MachineLink", "Machine Link runs a machine from storage", "machine-link", "playthrough"),
        F("item:ShippingLink", "Shipping Link ships from storage", "shipping-link", "playthrough"),
        F("item:CraftingRack", "Crafting Rack autocrafts", "autocraft", "playthrough"),
        F("item:IridiumDisk", "Iridium Disk crafted from a Gold Disk", "playthrough"),
        F("item:NetworkPort", "Network Ports join building networks", "playthrough"),
        F("item:PowerFeed", "Power Feed charges the Power Rack", "power-feed", "playthrough"),
        F("item:WirelessTerminal", "Wireless Terminal opens the network", "wireless", "playthrough"),
        F("item:AdvancedCraftingRack", "Advanced Crafting Rack keeps items in stock", "keep-stock", "playthrough"),
        F("item:PrismaticDisk", "Prismatic Disk crafted from an Iridium Disk", "playthrough"),
        F("item:IslandUplink", "Island Uplink joins Ginger Island", "playthrough"),
        F("item:QuestJournal", "The journal: mailed on day one, opens when used", "quest", "playthrough"),

        // Maru's letters.
        F("letter:T1", "one chat with Maru: components", "quest", "playthrough"),
        F("letter:T2", "10 processors, Mines 20, 2 hearts: storage", "quest", "playthrough"),
        F("letter:T3", "storage built, Mines 80, 5 hearts: automation", "playthrough"),
        F("letter:T4", "automation built, Mines 120, 7 hearts: autocrafting", "playthrough"),
        F("letter:T5", "autocrafting built, Skull Cavern 25, Iridium Bar, 8 hearts: linked buildings", "playthrough"),
        F("letter:T6", "linked buildings, island, walnuts, Prismatic Shard: whole farm", "playthrough"),
        F("letter:chapter", "journal chapter thank-you letters with rewards", "playthrough"),

        // Player-facing systems.
        F("wire.lay", "mouse and controller laying, top halves", "wire", "targeting", "wire-placement"),
        F("wire.where", "which tiles take wire (floor, flooring, soil, under objects) and which refuse it with the reason (walls, trees, water); the outline matches", "wire-placement"),
        F("wire.pickup", "pickaxe picks wire up", "wire", "heavy-cable"),
        F("wire.hide", "wrench hides and shows wire", "heavy-cable", "playthrough"),
        F("network.join", "blocks join one network; two Cores stop it", "network"),
        F("disk.insertRemove", "disks in and out with mouse and controller", "disks"),
        F("disk.upgrade", "crafting a disk keeps its ID and contents", "disk-upgrade"),
        F("terminal.storeWithdraw", "store and withdraw stacks and singles", "terminal", "playthrough"),
        F("storage.limits", "kind and item limits", "capacity"),
        F("tools.onDisks", "tools stored on disks keep working", "tools"),
        F("storage.anything", "any item goes on a disk: ones with their own state are kept whole and come back identical", "whole-items"),
        F("break.drops", "breaking blocks drops their contents", "break-rack", "placement"),
        F("power.basics", "batteries, fuel, unpowered networks", "power", "playthrough"),
        F("power.morning", "the morning power pass", "power-day", "playthrough"),
        F("autocraft.job", "autocraft jobs on the game clock", "autocraft", "playthrough"),
        F("autocraft.keepStock", "keep-in-stock overnight", "keep-stock", "playthrough"),
        F("press.refuse", "the press explains missing inputs", "press"),
        F("ports.buildings", "Ports beside the farmhouse and Sheds join one group", "playthrough"),
        F("uplink.island", "Uplinks join Ginger Island to the base", "playthrough"),
        F("crafting.menu", "every recipe crafts at the game's crafting menu", "playthrough"),
        F("crafting.qiGems", "recipes with Qi Gems spend them from the player's Qi Gem balance", "playthrough"),
        F("save.roundtrip", "mod data and the game's save format keep everything", "save"),
        F("progression.gates", "each tier letter waits for its last missing requirement", "playthrough"),
    };

    /// <summary>Feature ids from <paramref name="required"/> that have no entry (or an entry with no scenario).</summary>
    public static IEnumerable<string> Missing(IEnumerable<string> required)
    {
        var covered = Features.Where(f => f.Scenarios.Count > 0).Select(f => f.Id).ToHashSet();
        return required.Where(r => !covered.Contains(r));
    }

    /// <summary>Scenario names used in <see cref="Features"/> that aren't in <see cref="ScenarioIds"/>.</summary>
    public static IEnumerable<string> UnknownScenarios() =>
        Features.SelectMany(f => f.Scenarios).Distinct().Where(s => !ScenarioIds.Contains(s));
}
