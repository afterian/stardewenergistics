namespace StardewEnergistics
{
    /// <summary>
    /// Every namespaced string ID the mod uses, in one place. Lanes never invent IDs locally.
    /// Item IDs are unqualified; the game adds "(O)" for objects and "(BC)" for big craftables.
    /// </summary>
    internal static class Ids
    {
        public const string ModId = "afterian.StardewEnergistics";
        public const string Prefix = "afterian.StardewEnergistics_";

        // T1 components
        public const string ProcessorPress = Prefix + "ProcessorPress";     // big craftable
        public const string CopperProcessor = Prefix + "CopperProcessor";
        public const string GoldProcessor = Prefix + "GoldProcessor";
        public const string IridiumProcessor = Prefix + "IridiumProcessor";
        public const string CopperWire = Prefix + "CopperWire";
        public const string HeavyCable = Prefix + "HeavyCable";

        // T2 simple storage + basic crafting
        public const string NetworkWrench = Prefix + "NetworkWrench";
        public const string ComputerCore = Prefix + "ComputerCore";         // big craftable
        public const string StorageRack = Prefix + "StorageRack";           // big craftable
        public const string PowerRack = Prefix + "PowerRack";               // big craftable
        public const string CoalGenerator = Prefix + "CoalGenerator";       // big craftable
        public const string NetworkTerminal = Prefix + "NetworkTerminal";   // big craftable
        public const string ChestLink = Prefix + "ChestLink";
        public const string CopperDisk = Prefix + "CopperDisk";

        // T3 bigger storage + automation
        public const string IronDisk = Prefix + "IronDisk";
        public const string GoldDisk = Prefix + "GoldDisk";
        public const string MachineLink = Prefix + "MachineLink";
        public const string ShippingLink = Prefix + "ShippingLink";

        // T4 autocrafting
        public const string CraftingRack = Prefix + "CraftingRack";         // big craftable

        // T5 linked buildings
        public const string IridiumDisk = Prefix + "IridiumDisk";
        public const string NetworkPort = Prefix + "NetworkPort";           // big craftable
        public const string PowerFeed = Prefix + "PowerFeed";
        public const string WirelessTerminal = Prefix + "WirelessTerminal";

        // T6 whole farm
        public const string AdvancedCraftingRack = Prefix + "AdvancedCraftingRack"; // big craftable
        public const string PrismaticDisk = Prefix + "PrismaticDisk";
        public const string IslandUplink = Prefix + "IslandUplink";         // big craftable

        // Quest guide
        public const string QuestJournal = Prefix + "QuestJournal";

        /// <summary>Key prefix for values this mod stores in an object's modData.</summary>
        public const string ModDataPrefix = ModId + "/";

        /// <summary>modData keys. Block settings live on the object so they survive pick-up.</summary>
        public static class Data
        {
            /// <summary>On a disk item: its disk ID (contents are in save data under this ID).</summary>
            public const string DiskId = ModDataPrefix + "DiskId";
            /// <summary>On a Storage Rack: disk IDs in slot order, comma-separated.</summary>
            public const string RackDisks = ModDataPrefix + "Disks";
            /// <summary>On a Power Rack: charge of each Battery Pack in slot order, comma-separated.</summary>
            public const string RackCharge = ModDataPrefix + "Charge";
            /// <summary>On a Coal Generator: fuel loaded, in power units.</summary>
            public const string Fuel = ModDataPrefix + "Fuel";
            /// <summary>On a chest: "1" when a Chest Link is attached.</summary>
            public const string ChestLink = ModDataPrefix + "ChestLink";
            /// <summary>On a Computer Core: "1" powered, "0" not.</summary>
            public const string Powered = ModDataPrefix + "Powered";
            /// <summary>On a Computer Core: day the low-power warning was shown for the current drain.</summary>
            public const string LastWarningDay = ModDataPrefix + "LastWarningDay";
            /// <summary>On a machine: "1" when a Machine Link is attached.</summary>
            public const string MachineLink = ModDataPrefix + "MachineLink";
            /// <summary>On a Mini-Shipping Bin: "1" when a Shipping Link is attached.</summary>
            public const string ShippingLink = ModDataPrefix + "ShippingLink";
            /// <summary>On a linked machine or bin: the Link's settings (JSON).</summary>
            public const string LinkSettings = ModDataPrefix + "LinkSettings";
            /// <summary>On a linked machine: 0 not set up, 1 idle, 2 working, 3 missing input.</summary>
            public const string LinkState = ModDataPrefix + "LinkState";
            /// <summary>On a Lightning Rod or Solar Panel: "1" when a Power Feed is attached.</summary>
            public const string PowerFeed = ModDataPrefix + "PowerFeed";
            /// <summary>On an Advanced Crafting Rack: keep-in-stock rules, "count qualifiedId" separated by ';'.</summary>
            public const string StockRules = ModDataPrefix + "StockRules";
            /// <summary>On a Computer Core: the network's autocraft jobs (JSON).</summary>
            public const string Jobs = ModDataPrefix + "Jobs";
            /// <summary>On a machine: ID of the autocraft job it is working for.</summary>
            public const string ForJob = ModDataPrefix + "ForJob";
            /// <summary>On a player: quest guide goals done, comma-separated quest IDs.</summary>
            public const string GuideDone = ModDataPrefix + "GuideDone";
            /// <summary>On the farm: quest guide world flags seen so far (host writes), comma-separated names.</summary>
            public const string GuideWorld = ModDataPrefix + "GuideWorld";
            /// <summary>On the farm: tiers built so far, comma-separated tier numbers.</summary>
            public const string TiersBuilt = ModDataPrefix + "TiersBuilt";
        }

        /// <summary>Player stat counting Copper Processors pressed (T2 gate).</summary>
        public const string StatCopperProcessors = ModId + "_CopperProcessorsMade";
    }
}
