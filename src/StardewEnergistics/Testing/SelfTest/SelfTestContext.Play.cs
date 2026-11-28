using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Content;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// Play-tester helpers for things a player does outside one room: walking to other places, letting the game
/// clock run, crafting at the crafting menu, feeding machines, putting up buildings, and finding free floor.
/// </summary>
internal sealed partial class SelfTestContext
{
    // ---- Going places ----

    /// <summary>
    /// Walks (warps) the player to a location and makes it the one the helpers work in, with <paramref name="origin"/>
    /// as the top-left for <see cref="T"/>. Yield every step: <c>foreach (var s in c.Visit(...)) yield return s;</c>
    /// </summary>
    public IEnumerable<object?> Visit(GameLocation location, Vector2 origin, Vector2? stand = null)
    {
        Step($"go to {location.NameOrUniqueName}");
        CloseMenu();
        var at = stand ?? origin + new Vector2(0, 1);
        Loc = location;
        Origin = origin;
        if (Game1.currentLocation != location)
        {
            var request = Game1.getLocationRequest(location.NameOrUniqueName, location.isStructure.Value);
            Game1.warpFarmer(request, (int)at.X, (int)at.Y, Game1.up);
            yield return Wait.Until(() => Game1.currentLocation == location && !Game1.isWarping && !Game1.globalFade && Context.IsPlayerFree,
                $"arrive at {location.NameOrUniqueName}", 900);
        }
        yield return Wait.Ticks(2);
        StandAt(at, Game1.up);
        S.Networks.Clear();
    }

    /// <summary>Back to the scratch building (and its area) after visiting somewhere else.</summary>
    public IEnumerable<object?> GoHome() => Visit(Scratch, ScratchOrigin, ScratchOrigin + new Vector2(0, 2));

    // ---- Time ----

    /// <summary>
    /// Lets the game clock run <paramref name="tenMinutes"/> ten-minute steps the game's own way, so every
    /// handler that runs on the clock (the journal first, then Links, autocrafting and Power Feeds) runs
    /// in its real order. Keeps the clock before 10 PM so the player never passes out (which would save).
    /// </summary>
    public IEnumerable<object?> TimePasses(int tenMinutes = 1)
    {
        Step($"let {tenMinutes * 10} minutes pass on the game clock (from {Game1.timeOfDay})");
        for (int i = 0; i < tenMinutes; i++)
        {
            if (Game1.timeOfDay >= 2200)
            {
                Game1.timeOfDay = 600;
                yield return Wait.Ticks(2);
            }
            S.Networks.Clear();
            Game1.performTenMinuteClockUpdate();
            yield return Wait.Ticks(2); // SMAPI raises TimeChanged on the next tick
        }
        S.Networks.Clear();
    }

    /// <summary>Room for everything a long run picks up: a full 36-slot backpack, as after both upgrades.</summary>
    public void BigBackpack()
    {
        Player.MaxItems = Math.Max(Player.MaxItems, 36);
        while (Player.Items.Count < Player.MaxItems)
            Player.Items.Add(null);
    }

    /// <summary>Adds items to the backpack (outside the hand slot), as if picked up.</summary>
    public void Give(string qualifiedId, int count = 1)
    {
        while (count > 0)
        {
            int n = Math.Min(count, 999);
            var item = ItemRegistry.Create(qualifiedId, n);
            n = item.Stack;
            if (!Player.addItemToInventoryBool(item))
                Log($"backpack full: couldn't give {Short(qualifiedId)} x{n}", LogLevel.Warn);
            count -= Math.Max(1, n);
        }
    }

    /// <summary>Takes one of an item out of the backpack (null if there's none), e.g. to hold or use it.</summary>
    public Item? TakeFromBackpack(string qualifiedId)
    {
        for (int i = 0; i < Player.Items.Count; i++)
        {
            var item = Player.Items[i];
            if (item?.QualifiedItemId != qualifiedId)
                continue;
            if (item.Stack <= 1)
            {
                Player.Items[i] = null;
                return item;
            }
            var one = item.getOne();
            Player.Items[i] = item.ConsumeStack(1);
            return one;
        }
        return null;
    }

    /// <summary>Holds one of an item from the backpack (crafted earlier), or a new one if there's none.</summary>
    public Item HoldFromBackpack(string qualifiedId) => Hold(TakeFromBackpack(qualifiedId) ?? ItemRegistry.Create(qualifiedId));

    /// <summary>Takes items out of the backpack (to spend them on something the test doesn't play out).</summary>
    public void Remove(string qualifiedId, int count) => Player.Items.ReduceId(qualifiedId, count);

    // ---- Crafting, at the game's own crafting menu ----

    /// <summary>
    /// Crafts one batch of a known recipe the way a player does: opens the game's crafting menu, clicks the
    /// recipe, and puts the result in the backpack. Ingredients the backpack is short of are handed over first
    /// unless <paramref name="giveIngredients"/> is false (what the player carries, like a disk to upgrade, is used).
    /// Leaves the menu open (for a screenshot); returns what was made.
    /// </summary>
    public Item? Craft(string recipeId, bool giveIngredients = true)
    {
        Step($"craft {Short(recipeId)} at the crafting menu");
        CloseMenu();
        if (!Check($"{Short(recipeId)}: recipe known", Player.craftingRecipes.ContainsKey(recipeId)))
            return null;
        var recipe = new CraftingRecipe(recipeId, false);
        if (giveIngredients)
            foreach (var (id, count) in recipe.recipeList)
            {
                string q = ItemRegistry.QualifyItemId(id) ?? id;
                int missing = count - Player.Items.CountId(q);
                if (missing > 0)
                    Give(q, missing);
            }
        // Not recipe.createItem(): the disk-upgrade patch watches that call.
        string expected = (recipe.bigCraftable ? "(BC)" : "(O)") + recipe.itemToProduce[0];
        int timesBefore = Player.craftingRecipes[recipeId];

        int w = 800 + IClickableMenu.borderWidth * 2, h = 600 + IClickableMenu.borderWidth * 2;
        var pos = Utility.getTopLeftPositionForCenteringOnScreen(w, h);
        var page = new CraftingPage((int)pos.X, (int)pos.Y, w, h, false, true, null);
        Game1.activeClickableMenu = page;
        ClickableTextureComponent? button = null;
        for (int p = 0; p < page.pagesOfCraftingRecipes.Count && button == null; p++)
        {
            foreach (var (component, r) in page.pagesOfCraftingRecipes[p])
            {
                if (r.name == recipeId)
                {
                    page.currentCraftingPage = p;
                    button = component;
                    break;
                }
            }
        }
        if (!Check($"{Short(recipeId)}: shown in the crafting menu", button != null))
            return null;
        page.receiveLeftClick(button!.bounds.Center.X, button.bounds.Center.Y);
        var made = page.heldItem;
        page.heldItem = null;
        Check($"crafted {Short(expected)}", made?.QualifiedItemId == expected,
            $"got {made?.QualifiedItemId ?? "nothing"}; ingredients: {string.Join(", ", recipe.recipeList.Select(kv => $"{Short(kv.Key)} x{kv.Value} (have {Player.Items.CountId(ItemRegistry.QualifyItemId(kv.Key) ?? kv.Key)})"))}");
        CheckEqual($"{Short(recipeId)}: crafted count went up", timesBefore + 1, Player.craftingRecipes[recipeId]);
        if (made != null)
        {
            Log($"crafted {made.QualifiedItemId} x{made.Stack}");
            if (!Player.addItemToInventoryBool(made))
                Log($"backpack full: {made.QualifiedItemId} dropped", LogLevel.Warn);
        }
        return made;
    }

    /// <summary>Crafts a recipe <paramref name="times"/> times (ingredients handed over each time) and closes the menu.</summary>
    public void CraftTimes(string recipeId, int times)
    {
        for (int i = 0; i < times; i++)
            Craft(recipeId);
        CloseMenu();
    }

    // ---- The game's own action on a tile (machines, vanilla objects) ----

    /// <summary>
    /// The game's action-button handling on a tile (GameLocation.checkAction): puts the held item into a
    /// machine, collects a finished one, opens a chest. Use <see cref="Action"/> for the mod's own handlers.
    /// </summary>
    public bool GameAction(Vector2 tile)
    {
        StandBeside(tile);
        bool done = Loc.checkAction(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport, Player);
        Log($"game action on {Rel(tile)} -> {done}; held now {Player.CurrentItem?.QualifiedItemId ?? "nothing"}; menu {Game1.activeClickableMenu?.GetType().Name ?? "none"}");
        return done;
    }

    /// <summary>
    /// Presses processors at a Processor Press the way a player does: hold the bar, use it on the press (the
    /// other inputs come from the backpack), let it finish, collect. Hands over the inputs each time.
    /// Returns how many came out.
    /// </summary>
    public int PressProcessors(Vector2 pressTile, string barId, int count)
    {
        string bar = "(O)" + barId;
        int made = 0;
        for (int i = 0; i < count; i++)
        {
            if (!Loc.objects.TryGetValue(pressTile, out var press))
            {
                Check("Processor Press is there", false);
                return made;
            }
            foreach (var (id, n) in ProcessorPressOutput.ExtrasByBar[bar])
                Give(id, n);
            Hold(bar, 1);
            bool loaded = GameAction(pressTile);
            if (!Check($"press takes {Short(bar)} ({i + 1}/{count})", loaded && press.heldObject.Value != null, $"held {press.heldObject.Value?.QualifiedItemId ?? "nothing"}; {Messages()}"))
                return made;
            string output = press.heldObject.Value!.QualifiedItemId;
            press.minutesElapsed(Math.Max(10, press.MinutesUntilReady));
            Check($"press finished {Short(output)}", press.readyForHarvest.Value, $"minutes left {press.MinutesUntilReady}");
            HoldNothing();
            int before = Carried(output);
            GameAction(pressTile);
            if (Check($"collected {Short(output)} from the press", Carried(output) == before + 1, $"carried {Carried(output)}, was {before}"))
                made++;
        }
        return made;
    }

    // ---- Free space in real locations ----

    /// <summary>True if nothing is on the tile and it's open floor the player could stand on and place on.</summary>
    public bool IsFree(GameLocation loc, Vector2 t)
    {
        int x = (int)t.X, y = (int)t.Y;
        return loc.isTileOnMap(t) && !loc.isWaterTile(x, y) && loc.isTilePassable(t) && !loc.objects.ContainsKey(t)
            && loc.GetFurnitureAt(t) == null && (!loc.terrainFeatures.TryGetValue(t, out var tf) || tf is StardewValley.TerrainFeatures.Grass)
            && loc.getBuildingAt(t) == null
            && loc.getTileIndexAt(x, y, "Buildings") < 0 && loc.CanItemBePlacedHere(t) && !Game1.player.Tile.Equals(t);
    }

    /// <summary>True if the tile and its four neighbours have no wire or mod block, so new blocks there join nothing of the player's.</summary>
    public bool AwayFromNetworks(GameLocation loc, Vector2 t, ISet<Vector2>? own = null)
    {
        var layer = S.Wires.Find(loc);
        foreach (var d in new[] { Vector2.Zero, new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
        {
            var n = t + d;
            if (own?.Contains(n) == true)
                continue;
            if (layer?.Has(P(n)) == true)
                return false;
            if (d != Vector2.Zero && loc.objects.TryGetValue(n, out var o) && o.QualifiedItemId.Contains(Ids.Prefix))
                return false;
        }
        return true;
    }

    /// <summary>
    /// The left end of a free row of <paramref name="length"/> tiles with free floor below it to stand on,
    /// away from the player's own networks. Null if there's none.
    /// </summary>
    public Vector2? FindFreeRow(GameLocation loc, int length)
    {
        int w = loc.Map.Layers[0].LayerWidth, h = loc.Map.Layers[0].LayerHeight;
        for (int y = 1; y < h - 2; y++)
        {
            for (int x = 1; x + length < w - 1; x++)
            {
                bool ok = true;
                for (int dx = -1; dx <= length && ok; dx++)
                {
                    var top = new Vector2(x + dx, y);
                    bool inRow = dx >= 0 && dx < length;
                    ok = (!inRow || (IsFree(loc, top) && AwayFromNetworks(loc, top))) && IsFree(loc, new Vector2(x + dx, y + 1)) && AwayFromNetworks(loc, new Vector2(x + dx, y + 1));
                }
                if (ok)
                    return new Vector2(x, y);
            }
        }
        return null;
    }

    /// <summary>
    /// A path of free tiles from <paramref name="from"/> to <paramref name="to"/> (both included) that keeps
    /// clear of the player's own wire and blocks. Null if there's none.
    /// </summary>
    /// <param name="own">Tiles of this test's own blocks and wire, which the path may run beside.</param>
    public List<Vector2>? FindPath(GameLocation loc, Vector2 from, Vector2 to, ISet<Vector2>? own = null, int limit = 20000)
    {
        var prev = new Dictionary<Vector2, Vector2> { [from] = from };
        var queue = new Queue<Vector2>();
        queue.Enqueue(from);
        while (queue.Count > 0 && prev.Count < limit)
        {
            var at = queue.Dequeue();
            if (at == to)
            {
                var path = new List<Vector2> { to };
                while (path[^1] != from)
                    path.Add(prev[path[^1]]);
                path.Reverse();
                return path;
            }
            foreach (var d in new[] { new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1) })
            {
                var n = at + d;
                if (prev.ContainsKey(n))
                    continue;
                if (n != to && !(IsFree(loc, n) && AwayFromNetworks(loc, n, own)))
                    continue;
                prev[n] = at;
                queue.Enqueue(n);
            }
        }
        return null;
    }

    /// <summary>A free tile right beside a building (where a Network Port goes), away from other networks.</summary>
    public Vector2? FreeTileBeside(GameLocation outdoors, Building b)
    {
        foreach (var p in Networks.NetworkManager.TilesBeside(b))
        {
            var t = new Vector2(p.X, p.Y);
            if (IsFree(outdoors, t) && AwayFromNetworks(outdoors, t) && Networks.NetworkManager.BuildingBeside(outdoors, t) == b)
                return t;
        }
        return null;
    }

    /// <summary>
    /// Puts up a building on the farm the way Robin's finished construction leaves it, on the first free
    /// spot (with a tile of room all round). Returns the building or null.
    /// </summary>
    public Building? BuildOnFarm(string type)
    {
        var farm = Game1.getFarm();
        if (!Game1.buildingData.TryGetValue(type, out var data))
            return null;
        int w = data.Size.X, h = data.Size.Y;
        int mapW = farm.Map.Layers[0].LayerWidth, mapH = farm.Map.Layers[0].LayerHeight;
        var free = new bool[mapW, mapH];
        for (int fy = 0; fy < mapH; fy++)
            for (int fx = 0; fx < mapW; fx++)
                free[fx, fy] = IsFree(farm, new Vector2(fx, fy)) && AwayFromNetworks(farm, new Vector2(fx, fy))
                    && farm.doesTileHaveProperty(fx, fy, "NoBuild", "Back") == null;
        for (int y = 3; y + h + 2 < mapH; y++)
        {
            for (int x = 3; x + w + 2 < mapW; x++)
            {
                bool ok = true;
                for (int dy = -1; dy <= h && ok; dy++)
                    for (int dx = -1; dx <= w && ok; dx++)
                        ok = free[x + dx, y + dy];
                if (!ok)
                    continue;
                // Grass goes, as it does under a real building.
                for (int dy = 0; dy < h; dy++)
                    for (int dx = 0; dx < w; dx++)
                        farm.terrainFeatures.Remove(new Vector2(x + dx, y + dy));
                if (farm.buildStructure(type, new Vector2(x, y), Player, out var built, magicalConstruction: true, skipSafetyChecks: true))
                {
                    built.daysOfConstructionLeft.Value = 0;
                    if (built.GetIndoors() == null)
                        built.load();
                    Log($"built a {type} at ({x},{y}), indoors {built.GetIndoors()?.NameOrUniqueName ?? "none"}");
                    return built;
                }
            }
        }
        return null;
    }
}
