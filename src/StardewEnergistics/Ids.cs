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

        /// <summary>Key prefix for values this mod stores in an object's modData.</summary>
        public const string ModDataPrefix = ModId + "/";
    }
}
