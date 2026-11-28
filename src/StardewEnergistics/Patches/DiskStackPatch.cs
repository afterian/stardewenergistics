using HarmonyLib;
using StardewEnergistics.Storage;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Patches;

/// <summary>
/// Items that carry their own state never stack, so the state can't be merged away in the inventory:
/// disks (their contents), and picked-up blocks still holding disks, batteries, fuel, jobs or keep-in-stock rules.
/// </summary>
internal static class DiskStackPatch
{
    private static readonly string[] StateKeys =
    {
        Ids.Data.RackDisks,
        Ids.Data.RackCharge,
        Ids.Data.Fuel,
        Ids.Data.Jobs,
        Ids.Data.StockRules,
    };

    public static void Apply(Harmony harmony) => harmony.Patch(
        original: AccessTools.Method(typeof(SObject), nameof(SObject.maximumStackSize)),
        postfix: new HarmonyMethod(typeof(DiskStackPatch), nameof(Postfix)));

    private static void Postfix(SObject __instance, ref int __result)
    {
        if (__result > 1 && (DiskStore.IsDisk(__instance) || HoldsState(__instance)))
            __result = 1;
    }

    private static bool HoldsState(SObject obj)
    {
        if (!obj.QualifiedItemId.StartsWith("(BC)" + Ids.Prefix))
            return false;
        foreach (string key in StateKeys)
        {
            if (obj.modData.TryGetValue(key, out string? value) && !string.IsNullOrEmpty(value) && value != "0")
                return true;
        }
        return false;
    }
}
