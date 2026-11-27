using HarmonyLib;
using StardewEnergistics.Storage;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Patches;

/// <summary>Disks never stack: each one carries its own contents.</summary>
internal static class DiskStackPatch
{
    public static void Apply(Harmony harmony) => harmony.Patch(
        original: AccessTools.Method(typeof(SObject), nameof(SObject.maximumStackSize)),
        postfix: new HarmonyMethod(typeof(DiskStackPatch), nameof(Postfix)));

    private static void Postfix(SObject __instance, ref int __result)
    {
        if (DiskStore.IsDisk(__instance))
            __result = 1;
    }
}
