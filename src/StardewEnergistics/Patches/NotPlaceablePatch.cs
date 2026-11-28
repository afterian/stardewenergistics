using HarmonyLib;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Patches;

/// <summary>
/// Our small items (wire, Links, disks, the wrench…) are used on things, not placed as objects. Without this
/// the game treats them as placeable and draws its red/green placement square, which showed red on a chest
/// or machine even though using a Link there works. Items saved before the data change still say "Crafting",
/// so this checks the item ID rather than relying on the data alone.
/// </summary>
internal static class NotPlaceablePatch
{
    public static void Apply(Harmony harmony) => harmony.Patch(
        original: AccessTools.Method(typeof(SObject), nameof(SObject.isPlaceable)),
        postfix: new HarmonyMethod(typeof(NotPlaceablePatch), nameof(Postfix)));

    private static void Postfix(SObject __instance, ref bool __result)
    {
        if (__result && __instance.QualifiedItemId.StartsWith("(O)" + Ids.Prefix))
            __result = false;
    }
}
