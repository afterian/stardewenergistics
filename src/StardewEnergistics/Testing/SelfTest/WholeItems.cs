using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Storage;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// Anything goes on a disk: items with their own state (an enchanted tool, an enchanted weapon, a ring, a hat,
/// boots, flavoured wine, a coloured flower, an item another mod tagged) are stored whole through the Terminal and
/// come back identical. A disk can't go inside a disk.
/// </summary>
internal static class WholeItems
{
    private sealed record Case(string Name, Func<Item> Make, Func<Item, bool> Same);

    public static IEnumerable<object?> Run(SelfTestContext c)
    {
        c.BuildBasicNetwork();
        var disk = c.NewDisk(Ids.GoldDisk);
        c.InsertDisk(c.T(1, 0), disk);
        string diskId = SelfTestContext.DiskIdOf(disk);
        var term = c.T(3, 0);

        var cases = new List<Case>
        {
            new("enchanted Pickaxe", () =>
            {
                var t = new Pickaxe();
                t.AddEnchantment(new StardewValley.Enchantments.PowerfulEnchantment());
                return t;
            }, i => i is Pickaxe p && p.enchantments.Any(e => e is StardewValley.Enchantments.PowerfulEnchantment)),
            new("enchanted sword", () =>
            {
                var w = (MeleeWeapon)ItemRegistry.Create("(W)0");
                w.AddEnchantment(new StardewValley.Enchantments.CrusaderEnchantment());
                return w;
            }, i => i is MeleeWeapon w && w.enchantments.Any(e => e is StardewValley.Enchantments.CrusaderEnchantment)),
            new("ring", () => ItemRegistry.Create("(O)516"), i => i is Ring),
            new("hat", () => ItemRegistry.Create("(H)0"), i => i is Hat),
            new("boots", () => ItemRegistry.Create("(B)504"), i => i is Boots),
            new("flavoured wine x3", () =>
            {
                var wine = ItemRegistry.GetObjectTypeDefinition().CreateFlavoredWine(ItemRegistry.Create<SObject>("(O)613"));
                wine.Stack = 3;
                return wine;
            }, i => i is SObject o && o.preservedParentSheetIndex.Value == "613" && o.Stack == 3),
            new("coloured flower x2", () => new ColoredObject("597", 2, Color.Red), i => i is ColoredObject co && co.color.Value == Color.Red && co.Stack == 2),
            new("item tagged by another mod", () =>
            {
                var o = ItemRegistry.Create<SObject>("(O)390", 5);
                o.modData["another.mod/tag"] = "kept";
                return o;
            }, i => i.modData.TryGetValue("another.mod/tag", out string? v) && v == "kept" && i.Stack == 5),
        };

        var menu = c.OpenTerminal(term);
        c.Require("Terminal opens", menu != null);
        const int slot = 3;

        foreach (var k in cases)
        {
            c.Step($"store and withdraw: {k.Name}");
            var item = k.Make();
            string id = item.QualifiedItemId;
            int before = c.Carried(id);
            c.Player.Items[slot] = item;
            c.ClickInventorySlot(menu!, slot);
            c.CheckEqual($"{k.Name}: left the backpack", before, c.Carried(id));
            var storage = c.StorageAt(term)!;
            c.Check($"{k.Name}: stored whole on the disk", DiskStore.WholeItems(diskId).Any(i => i != null && i.QualifiedItemId == id),
                string.Join(", ", DiskStore.WholeItems(diskId).Where(i => i != null).Select(i => i!.QualifiedItemId)));
            c.Check($"{k.Name}: listed as a disk row", storage.Rows().Any(r => r.DiskId == diskId && r.Display.QualifiedItemId == id));
            yield return null;

            c.Check($"{k.Name}: shown in the Terminal", c.ClickStoredItem(menu!, id));
            var back = c.CarriedItems(id).FirstOrDefault(i => k.Same(i));
            c.Check($"{k.Name}: comes back with all its state", back != null,
                string.Join(", ", c.CarriedItems(id).Select(i => $"{i.GetType().Name} x{i.Stack}")));
            c.Check($"{k.Name}: gone from the disk", !DiskStore.WholeItems(diskId).Any(i => i != null && i.QualifiedItemId == id));
            if (back != null)
                c.Player.removeItemFromInventory(back);
            yield return null;
        }

        c.Step("disk space is counted and freed");
        var d = c.S.Disks.Get(diskId, Core.Storage.DiskTier.Gold);
        c.CheckEqual("nothing left on the disk", 0L, d.TotalItems);
        c.Player.Items[slot] = ItemRegistry.Create("(O)516");
        c.ClickInventorySlot(menu!, slot);
        c.CheckEqual("a stored ring counts as 1 item", 1L, d.TotalItems);
        c.CheckEqual("and 1 kind", 1, d.KindsUsed);
        yield return null;
        c.ClickStoredItem(menu!, "(O)516");
        c.CheckEqual("withdrawing it frees the space", 0L, d.TotalItems);
        foreach (var ring in c.CarriedItems("(O)516").ToList())
            c.Player.removeItemFromInventory(ring);
        yield return null;

        c.Step("a disk can't go inside a disk");
        var other = c.NewDisk();
        c.Player.Items[slot] = other;
        c.ClickInventorySlot(menu!, slot);
        c.Check("the disk stays in the backpack", c.Player.Items.Contains(other));
        c.Player.removeItemFromInventory(other);
        c.CloseMenu();
    }
}
