using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Graph;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Crafting;
using StardewEnergistics.Machines;
using StardewEnergistics.Networks;
using StardewEnergistics.Power;
using StardewEnergistics.Storage;
using StardewEnergistics.World;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>The mod's live services, handed to the self-test by ModEntry.</summary>
internal sealed class SelfTestServices
{
    public WireLayerStore Wires { get; init; } = null!;
    public NetworkManager Networks { get; init; } = null!;
    public DiskStore Disks { get; init; } = null!;
    public PowerService Power { get; init; } = null!;
    public CraftingService Crafting { get; init; } = null!;
    public MachineLinkService Links { get; init; } = null!;

    /// <summary>Objects with an OnButtonPressed handler, in the order ModEntry registers them.</summary>
    public IReadOnlyList<object> InputHandlers { get; init; } = System.Array.Empty<object>();
}

/// <summary>One check's outcome.</summary>
internal sealed record CheckResult(string Scenario, string Name, bool Passed, string Detail);

/// <summary>Thrown by <see cref="SelfTestContext.Require"/> to stop a scenario whose later steps depend on a failed check.</summary>
internal sealed class ScenarioAbort : Exception
{
    public ScenarioAbort(string message) : base(message) { }
}

/// <summary>Waits the scheduler understands when a scenario yields them.</summary>
internal static class Wait
{
    /// <summary>Wait this many update ticks.</summary>
    public static object Ticks(int n) => n;

    /// <summary>Wait until the condition is true (fails the scenario after <paramref name="timeoutTicks"/>).</summary>
    public static object Until(Func<bool> condition, string what, int timeoutTicks = 600) => new WaitUntil(condition, what, timeoutTicks);
}

internal sealed record WaitUntil(Func<bool> Condition, string What, int TimeoutTicks);

/// <summary>
/// What a scenario works with: a scratch location, the player, synthetic input and checks.
/// Everything is done through the same code paths a player's actions take: the game's placement,
/// tools and menus, and the mod's own input handlers.
/// </summary>
internal sealed class SelfTestContext
{
    public const string LocationName = "afterian.StardewEnergistics_SelfTest";

    private readonly IMonitor monitor;
    private readonly List<CheckResult> results;

    public SelfTestContext(IModHelper helper, IMonitor monitor, SelfTestServices services, GameLocation location, Vector2 origin, bool throwaway, List<CheckResult> results)
    {
        Helper = helper;
        this.monitor = monitor;
        S = services;
        Loc = location;
        Origin = origin;
        Throwaway = throwaway;
        this.results = results;
        Input = new SyntheticInput(helper, services.InputHandlers);
    }

    public IModHelper Helper { get; }

    public SelfTestServices S { get; }

    /// <summary>The scratch location every scenario builds in.</summary>
    public GameLocation Loc { get; }

    /// <summary>Top-left tile of the free area (at least <see cref="AreaWidth"/> x <see cref="AreaHeight"/>).</summary>
    public Vector2 Origin { get; }

    public const int AreaWidth = 10;
    public const int AreaHeight = 6;

    /// <summary>True when the session is a throwaway (launched for the self-test, never saved).</summary>
    public bool Throwaway { get; }

    public SyntheticInput Input { get; }

    /// <summary>Screenshots for the showcase run (set by the runner).</summary>
    public Screenshots? Shots { get; set; }

    /// <summary>
    /// Showcase: pause so a person can follow along, and save a screenshot with this caption.
    /// Yield the result: <c>yield return c.Show("...")</c>.
    /// </summary>
    public object Show(string caption, int pauseTicks = 75)
    {
        Shots?.Request(caption);
        return Wait.Ticks(pauseTicks);
    }

    /// <summary>The scenario now running (set by the runner).</summary>
    public string Scenario { get; set; } = "";

    /// <summary>Disk IDs the tests made, emptied at the end so nothing is left in save data.</summary>
    public HashSet<string> DiskIds { get; } = new();

    public Farmer Player => Game1.player;

    /// <summary>Tile at an offset from the area's top-left.</summary>
    public Vector2 T(int dx, int dy) => Origin + new Vector2(dx, dy);

    public static TilePos P(Vector2 v) => new((int)v.X, (int)v.Y);

    // ---- Checks ----

    public bool Check(string name, bool passed, string detail = "")
    {
        results.Add(new CheckResult(Scenario, name, passed, detail));
        monitor.Log($"[self-test] {(passed ? "PASS" : "FAIL")} {Scenario}: {name}{(detail.Length > 0 ? " (" + detail + ")" : "")}", passed ? LogLevel.Trace : LogLevel.Warn);
        return passed;
    }

    public bool CheckEqual<T>(string name, T expected, T actual)
    {
        bool ok = EqualityComparer<T>.Default.Equals(expected, actual);
        return Check(name, ok, $"expected {expected}, got {actual}");
    }

    /// <summary>A check whose failure makes the rest of the scenario meaningless.</summary>
    public void Require(string name, bool passed, string detail = "")
    {
        if (!Check(name, passed, detail))
            throw new ScenarioAbort($"stopped after failed check '{name}'");
    }

    // ---- Area and player ----

    /// <summary>Empties the scratch area: objects, wire, debris, menus, HUD messages and the backpack.</summary>
    public void Reset()
    {
        if (Game1.activeClickableMenu != null)
            Game1.exitActiveMenu();
        Loc.objects.Clear();
        Loc.debris.Clear();
        Loc.terrainFeatures.Clear();
        S.Wires.For(Loc).Clear();
        S.Networks.Clear();
        Game1.hudMessages.Clear();
        ClearBackpack();
        StandAt(T(0, 2), Game1.up);
    }

    public void ClearBackpack()
    {
        for (int i = 0; i < Player.Items.Count; i++)
            Player.Items[i] = null;
        Player.CurrentToolIndex = 0;
    }

    /// <summary>Moves the player onto a tile, facing a direction (Game1.up/down/left/right).</summary>
    public void StandAt(Vector2 tile, int facing)
    {
        Player.Position = tile * Game1.tileSize;
        Player.FacingDirection = facing;
        Player.Halt();
    }

    /// <summary>
    /// The backpack slot used as "the hand": the last toolbar slot, so items that land in the first free
    /// slots (withdrawals, pick-ups) are never overwritten by what a scenario holds next.
    /// </summary>
    public const int HandSlot = 11;

    /// <summary>Puts an item in the hand slot and selects it.</summary>
    public Item Hold(Item item)
    {
        Player.Items[HandSlot] = item;
        Player.CurrentToolIndex = HandSlot;
        return item;
    }

    public Item Hold(string qualifiedId, int count = 1) => Hold(ItemRegistry.Create(qualifiedId, count));

    public void HoldNothing()
    {
        Player.Items[HandSlot] = null;
        Player.CurrentToolIndex = HandSlot;
    }

    /// <summary>How many of an item the player carries.</summary>
    public int Carried(string qualifiedId) => Player.Items.Where(i => i?.QualifiedItemId == qualifiedId).Sum(i => i!.Stack);

    public IEnumerable<Item> CarriedItems(string qualifiedId) => Player.Items.Where(i => i?.QualifiedItemId == qualifiedId).Select(i => i!);

    /// <summary>A new disk item with its disk ID assigned (and tracked for clean-up).</summary>
    public Item NewDisk(string diskItemId = Ids.CopperDisk)
    {
        var disk = ItemRegistry.Create("(O)" + diskItemId);
        DiskIds.Add(S.Disks.ForItem(disk).Id.Value);
        return disk;
    }

    public static string DiskIdOf(Item disk) => disk.modData.TryGetValue(Ids.Data.DiskId, out string? id) ? id : "";

    // ---- Actions, through the real code paths ----

    /// <summary>Places a held item on a tile the way the game does when the player uses it (Utility.tryToPlaceItem).</summary>
    public SObject? Place(string qualifiedId, Vector2 tile)
    {
        StandBeside(tile);
        var item = Hold(qualifiedId);
        bool placed = Utility.tryToPlaceItem(Loc, (SObject)item, (int)tile.X * Game1.tileSize + 32, (int)tile.Y * Game1.tileSize + 32);
        Check($"place {Short(qualifiedId)} at {Rel(tile)}", placed && Loc.objects.ContainsKey(tile), placed ? "" : "tryToPlaceItem returned false");
        return Loc.objects.TryGetValue(tile, out var obj) ? obj : null;
    }

    /// <summary>Stand on the free tile below (or beside) a target so it's in reach.</summary>
    public void StandBeside(Vector2 tile)
    {
        foreach (var (d, facing) in new[] { (new Vector2(0, 1), Game1.up), (new Vector2(-1, 0), Game1.right), (new Vector2(1, 0), Game1.left), (new Vector2(0, -1), Game1.down) })
        {
            var at = tile + d;
            if (!Loc.objects.ContainsKey(at) && Loc.isTilePassable(at))
            {
                StandAt(at, facing);
                return;
            }
        }
        StandAt(tile + new Vector2(0, 1), Game1.up);
    }

    /// <summary>Lays wire from the hand by pressing the tool button with the mouse cursor on each tile.</summary>
    public int LayWire(IEnumerable<Vector2> tiles, string wireId = Ids.CopperWire)
    {
        var list = tiles.ToList();
        Hold("(O)" + wireId, list.Count + 5);
        int laid = 0;
        foreach (var t in list)
        {
            StandBeside(t);
            if (Input.Press(SButton.MouseLeft, t) && S.Wires.For(Loc).Has(P(t)))
                laid++;
        }
        S.Networks.Invalidate(Loc);
        return laid;
    }

    public IEnumerable<Vector2> Row(int y, int fromX, int toX)
    {
        for (int x = fromX; x <= toX; x++)
            yield return T(x, y);
    }

    /// <summary>The action button (right-click / controller A) on a tile, as a mouse or a controller with no cursor.</summary>
    public bool Action(Vector2 tile, bool mouse = true) =>
        mouse ? Input.Press(SButton.MouseRight, tile, GrabFor(tile)) : Input.Press(SButton.ControllerA, tile, Player.GetGrabTile(), mouse: false);

    /// <summary>The tool button (left-click / controller X).</summary>
    public bool UseTool(Vector2 tile, bool mouse = true) =>
        mouse ? Input.Press(SButton.MouseLeft, tile, GrabFor(tile)) : Input.Press(SButton.ControllerX, tile, Player.GetGrabTile(), mouse: false);

    /// <summary>SMAPI's grab tile: the cursor tile when it's within one tile of the player, else the tile in front.</summary>
    private Vector2 GrabFor(Vector2 cursor)
    {
        var me = Player.Tile;
        return Math.Abs(cursor.X - me.X) <= 1 && Math.Abs(cursor.Y - me.Y) <= 1 ? cursor : Player.GetGrabTile();
    }

    /// <summary>Hits a placed object with a tool the way the game's tool use does, until it comes off (at most 6 hits).</summary>
    public bool Break(Vector2 tile, Tool tool)
    {
        StandBeside(tile);
        Hold(tool);
        Player.Stamina = Player.MaxStamina;
        for (int hit = 0; hit < 6 && Loc.objects.ContainsKey(tile); hit++)
        {
            tool.DoFunction(Loc, (int)tile.X * Game1.tileSize + 32, (int)tile.Y * Game1.tileSize + 32, 1, Player);
            Player.Stamina = Player.MaxStamina;
        }
        return !Loc.objects.ContainsKey(tile);
    }

    /// <summary>Items lying on the ground in the scratch location.</summary>
    public List<Item> Debris() => Loc.debris.Where(d => d.item != null).Select(d => d.item).ToList();

    /// <summary>Picks every dropped item up into the backpack (as walking over it would) and clears the ground.</summary>
    public List<Item> PickUpDebris()
    {
        var items = Debris();
        Loc.debris.Clear();
        foreach (var item in items)
            Player.addItemToInventoryBool(item);
        return items;
    }

    // ---- Network state ----

    public LocationNetworks Nets => S.Networks.For(Loc);

    public Network? NetworkAt(Vector2 tile) => Nets.NetworkAt(tile);

    /// <summary>The storage the Terminal at (or any block on) this tile uses, or null if the tile isn't on a network.</summary>
    public NetworkStorage? StorageAt(Vector2 tile)
    {
        var loc = Nets;
        var n = loc.NetworkAt(tile);
        if (n == null)
            return null;
        var home = S.Networks.Home(loc, n);
        return home is { } h ? NetworkStorage.For(h.Loc, h.Net, S.Disks) : NetworkStorage.For(loc, n, S.Disks);
    }

    public static long Stored(NetworkStorage? storage, string qualifiedId, int quality = 0) => storage?.Count(new ItemKey(qualifiedId, quality)) ?? 0;

    /// <summary>Builds the standard powered network used by most scenarios.
    /// Row 0: Core, Storage Rack, Power Rack, Terminal at x = 0..3, wire under all of them; the player stands on row 2.</summary>
    public (SObject Core, SObject Rack, SObject Power, SObject Terminal) BuildBasicNetwork(int batteries = 2)
    {
        LayWire(Row(0, 0, 5));
        var core = Place("(BC)" + Ids.ComputerCore, T(0, 0))!;
        var rack = Place("(BC)" + Ids.StorageRack, T(1, 0))!;
        var power = Place("(BC)" + Ids.PowerRack, T(2, 0))!;
        var terminal = Place("(BC)" + Ids.NetworkTerminal, T(3, 0))!;
        if (core == null || rack == null || power == null || terminal == null)
            throw new ScenarioAbort("couldn't place the basic network");
        for (int i = 0; i < batteries; i++)
        {
            StandBeside(T(2, 0));
            Hold("(O)" + Content.VanillaIds.BatteryPack);
            Action(T(2, 0));
        }
        S.Networks.Invalidate(Loc);
        return (core, rack, power, terminal);
    }

    /// <summary>Puts a disk into a Storage Rack by using it on the rack.</summary>
    public bool InsertDisk(Vector2 rackTile, Item disk, bool mouse = true)
    {
        StandBeside(rackTile);
        Hold(disk);
        bool taken = Action(rackTile, mouse);
        S.Networks.Invalidate(Loc);
        return taken;
    }

    public static string Short(string qualifiedId) => qualifiedId.Replace(Ids.Prefix, "");

    public string Rel(Vector2 tile) => $"({tile.X - Origin.X},{tile.Y - Origin.Y})";

    // ---- Terminal menu ----

    /// <summary>Opens the Terminal on a tile with the action button; returns its menu or null.</summary>
    public IClickableMenu? OpenTerminal(Vector2 tile)
    {
        StandBeside(tile);
        HoldNothing();
        Action(tile);
        return Game1.activeClickableMenu?.GetType().Name == "TerminalMenu" ? Game1.activeClickableMenu : null;
    }

    /// <summary>Clicks a backpack slot in an open Terminal (left = whole stack, right = one).</summary>
    public void ClickInventorySlot(IClickableMenu menu, int slot, bool right = false)
    {
        var inv = Helper.Reflection.GetField<InventoryMenu>(menu, "inventory").GetValue();
        var at = inv.inventory[slot].bounds.Center;
        if (right)
            menu.receiveRightClick(at.X, at.Y);
        else
            menu.receiveLeftClick(at.X, at.Y);
    }

    /// <summary>Clicks the Terminal grid cell showing an item (left = a stack, right = one). False if it isn't shown.</summary>
    public bool ClickStoredItem(IClickableMenu menu, string qualifiedId, bool right = false)
    {
        var shown = Helper.Reflection.GetField<System.Collections.IList>(menu, "shown").GetValue();
        int scroll = Helper.Reflection.GetField<int>(menu, "scrollRow").GetValue();
        var grid = Helper.Reflection.GetField<List<ClickableComponent>>(menu, "gridSlots").GetValue();
        for (int i = 0; i < shown.Count; i++)
        {
            var display = (Item)shown[i]!.GetType().GetProperty("Display")!.GetValue(shown[i])!;
            if (display.QualifiedItemId != qualifiedId)
                continue;
            int cell = i - scroll * 12;
            if (cell < 0 || cell >= grid.Count)
                return false;
            var at = grid[cell].bounds.Center;
            if (right)
                menu.receiveRightClick(at.X, at.Y);
            else
                menu.receiveLeftClick(at.X, at.Y);
            return true;
        }
        return false;
    }

    /// <summary>Backpack slot holding an item, or -1.</summary>
    public int SlotOf(string qualifiedId)
    {
        for (int i = 0; i < Player.Items.Count; i++)
            if (Player.Items[i]?.QualifiedItemId == qualifiedId)
                return i;
        return -1;
    }

    /// <summary>The latest HUD or red message text (for "why didn't that work" checks).</summary>
    public string LastMessage() => Game1.hudMessages.Count > 0 ? Game1.hudMessages[^1].message ?? "" : "";

    /// <summary>True if any HUD message on screen has this text.</summary>
    public bool Showed(string text) => Game1.hudMessages.Any(m => m.message == text);

    public string Messages() => string.Join(" | ", Game1.hudMessages.Select(m => m.message));

    public string T(string key) => Helper.Translation.Get(key);
}
