using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Progression;
using StardewEnergistics.Storage;
using StardewValley;
using static StardewEnergistics.Content.VanillaIds;

namespace StardewEnergistics.Testing;

/// <summary>
/// Cheats shared by the se_ console commands and the in-game test menu. Everything acts on the
/// local player only (their own recipes and backpack), so it works the same for the host and farmhands.
/// </summary>
internal static class TestTools
{
    /// <summary>Teach the local player every recipe (or one tier's). Returns the recipes newly learned.</summary>
    public static List<string> Unlock(TierId? tier = null)
    {
        var ids = tier is { } t ? ContentInjector.RecipesFor(t).ToList() : ContentInjector.AllRecipes.ToList();
        var added = new List<string>();
        foreach (string id in ids)
        {
            if (!Game1.player.craftingRecipes.ContainsKey(id))
            {
                Game1.player.craftingRecipes.Add(id, 0);
                added.Add(id);
            }
        }
        return added;
    }

    /// <summary>Qualified ID for a mod item's short name (e.g. "CopperDisk"), or null if there's no such item.</summary>
    public static string? QualifiedFromName(string name)
    {
        string id = Ids.Prefix + name;
        if (ItemRegistry.GetData("(O)" + id) != null)
            return "(O)" + id;
        if (ItemRegistry.GetData("(BC)" + id) != null)
            return "(BC)" + id;
        return null;
    }

    /// <summary>Create items for a give: disks and other non-stacking items come as separate items.</summary>
    public static IEnumerable<Item> Make(string qualifiedId, int count)
    {
        var probe = ItemRegistry.Create(qualifiedId);
        int each = DiskStore.IsDisk(probe) ? 1 : System.Math.Max(1, probe.maximumStackSize());
        for (int left = count; left > 0; left -= each)
            yield return ItemRegistry.Create(qualifiedId, System.Math.Min(each, left));
    }

    /// <summary>Put items in the backpack; anything that doesn't fit opens the usual "take these" menu.</summary>
    public static void Give(IEnumerable<Item> items)
    {
        var list = items.ToList();
        if (list.Count > 0)
            Game1.player.addItemsByMenuIfNecessary(list);
    }

    public static void Give(string qualifiedId, int count) => Give(Make(qualifiedId, count));

    private static string O(string id) => "(O)" + id;

    private static string BC(string id) => "(BC)" + id;

    /// <summary>Everything needed to build and test a working network in one building.</summary>
    public static readonly (string Id, int Count)[] StarterKit =
    {
        (BC(Ids.ComputerCore), 1),
        (BC(Ids.StorageRack), 2),
        (BC(Ids.PowerRack), 1),
        (BC(Ids.CoalGenerator), 1),
        (BC(Ids.NetworkTerminal), 1),
        (BC(Ids.CraftingRack), 1),
        (BC(Ids.ProcessorPress), 1),
        (O(Ids.NetworkWrench), 1),
        (O(Ids.CopperWire), 200),
        (O(Ids.HeavyCable), 100),
        (O(Ids.CopperDisk), 2),
        (O(Ids.IronDisk), 1),
        (O(Ids.ChestLink), 5),
        (O(Ids.MachineLink), 5),
        (O(Ids.ShippingLink), 1),
        (O(Ids.CopperProcessor), 20),
        (O(Ids.GoldProcessor), 10),
        (O(Ids.IridiumProcessor), 5),
        (O(BatteryPack), 10),
        (O(Coal), 50),
        (BC("130"), 4), // Chest
    };

    /// <summary>Vanilla materials for crafting every mod recipe at a Terminal or the crafting page.</summary>
    public static readonly (string Id, int Count)[] Materials =
    {
        (O(CopperBar), 100),
        (O(IronBar), 100),
        (O(GoldBar), 100),
        (O(IridiumBar), 50),
        (O(RefinedQuartz), 50),
        (O(BatteryPack), 20),
        (O(Coal), 100),
        (O(Hardwood), 50),
        (O(Cloth), 30),
        (O(Fiber), 100),
        (O(Stone), 200),
        (O(Diamond), 10),
        (O(PrismaticShard), 5),
        (O(QiGem), 30),
        (O(RadioactiveBar), 20),
        (BC(FurnaceBig), 2),
    };

    public static void GiveSet(IEnumerable<(string Id, int Count)> set) =>
        Give(set.Where(e => ItemRegistry.GetData(e.Id) != null).SelectMany(e => Make(e.Id, e.Count)));
}
