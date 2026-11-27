namespace StardewEnergistics.Content;

/// <summary>Vanilla item IDs used in recipes and machine rules (unqualified object IDs).</summary>
internal static class VanillaIds
{
    public const string Diamond = "72";
    public const string CopperBar = "334";
    public const string IronBar = "335";
    public const string GoldBar = "336";
    public const string IridiumBar = "337";
    public const string RefinedQuartz = "338";
    public const string Cloth = "428";
    public const string Fiber = "771";
    public const string Stone = "390";
    public const string Coal = "382";
    public const string Hardwood = "709";
    public const string BatteryPack = "787";

    /// <summary>Big craftable ID of the Furnace.</summary>
    public const string FurnaceBig = "13";

    public static string Obj(string id) => "(O)" + id;

    public static string Big(string id) => "(BC)" + id;
}
