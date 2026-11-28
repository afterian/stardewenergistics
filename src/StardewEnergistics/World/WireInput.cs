using Microsoft.Xna.Framework;
using StardewEnergistics.Core.Geometry;
using StardewEnergistics.Core.Wires;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace StardewEnergistics.World;

/// <summary>
/// Player input for wire:
/// <list type="bullet">
/// <item>Holding Copper Wire or Heavy Cable: click a floor tile to lay one; hold and drag to lay a line.
/// Clicking a chest or machine lays wire under it (the block's own tile is where it connects).</item>
/// <item>Pickaxe on a bare wire tile picks the wire back up, like a path.</item>
/// <item>Network Wrench: tap to hide/show one tile; hold to hide/show the whole run;
/// right-click Heavy Cable to change its colour. Holding the wrench shows Network View.</item>
/// </list>
/// </summary>
internal sealed class WireInput
{
    private readonly IModHelper helper;
    private readonly ModConfig config;
    private readonly WireLayerStore store;
    private readonly WireRenderer renderer;

    /// <summary>Input state per split-screen player.</summary>
    private sealed class State
    {
        public SButton? DragButton;
        public TilePos? LastDragTile;
        public SButton? WrenchButton;
        public TilePos WrenchTile;
        public int WrenchHeldTicks;
        public bool WrenchRunDone;
        public bool NetworkViewToggled;
    }

    private readonly PerScreen<State> state = new(() => new State());

    public WireInput(IModHelper helper, ModConfig config, WireLayerStore store, WireRenderer renderer)
    {
        this.helper = helper;
        this.config = config;
        this.store = store;
        this.renderer = renderer;
    }

    public void Register()
    {
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Input.ButtonReleased += OnButtonReleased;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
    }

    /// <summary>While holding wire, outline the tile it would go on (green if it can, red if not).</summary>
    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsPlayerFree || HeldWireKind() == null)
            return;
        var tile = WireTile(helper.Input.GetCursorPosition());
        bool ok = CanLay(Game1.currentLocation, tile) && store.Find(Game1.currentLocation)?.Get(tile) == null;
        var screen = Game1.GlobalToLocal(Game1.viewport, new Vector2(tile.X * 64, tile.Y * 64));
        e.SpriteBatch.Draw(Game1.mouseCursors, screen, new Rectangle(ok ? 194 : 210, 388, 16, 16), Color.White * 0.7f, 0f, Vector2.Zero, 4f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0.01f);
    }

    private static WireKind? HeldWireKind()
    {
        return Game1.player.ActiveObject?.QualifiedItemId switch
        {
            "(O)" + Ids.CopperWire => WireKind.Copper,
            "(O)" + Ids.HeavyCable => WireKind.Heavy,
            _ => null,
        };
    }

    private static bool HoldingWrench() => Game1.player.ActiveObject?.QualifiedItemId == "(O)" + Ids.NetworkWrench;

    private static TilePos ToTile(Vector2 v) => new((int)v.X, (int)v.Y);

    /// <summary>How far from the player (in tiles) wire can be laid at the cursor.</summary>
    public const int WireReach = 5;

    /// <summary>
    /// The tile wire goes on: the tile under the visible cursor (mouse, trackpad or the Deck's stick cursor)
    /// when it's within <see cref="WireReach"/> tiles; otherwise the tile in front of the player.
    /// SMAPI's GrabTile alone falls back to the tile in front as soon as the cursor is over 1 tile away,
    /// which made wire land somewhere other than the cursor.
    /// </summary>
    private static TilePos WireTile(ICursorPosition cursor)
    {
        var t = cursor.Tile;
        var me = Game1.player.Tile;
        bool cursorShown = Game1.mouseCursorTransparency > 0 && Game1.wasMouseVisibleThisFrame;
        if (cursorShown && System.Math.Abs(t.X - me.X) <= WireReach && System.Math.Abs(t.Y - me.Y) <= WireReach)
            return ToTile(t);
        return ToTile(cursor.GrabTile);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        var st = state.Value;
        if (config.NetworkViewKey.JustPressed() && Context.IsWorldReady)
        {
            st.NetworkViewToggled = !st.NetworkViewToggled;
            Game1.addHUDMessage(new HUDMessage(helper.Translation.Get(st.NetworkViewToggled ? "networkView.on" : "networkView.off")) { noIcon = true });
            return;
        }

        if (!Context.IsPlayerFree)
            return;

        var location = Game1.currentLocation;
        var tile = ToTile(e.Cursor.GrabTile);
        bool useTool = e.Button.IsUseToolButton();
        bool action = e.Button.IsActionButton();

        // Lay wire.
        if (HeldWireKind() is { } kind && (useTool || action))
        {
            tile = WireTile(e.Cursor);
            helper.Input.Suppress(e.Button);
            if (TryLay(location, tile, kind))
            {
                st.DragButton = e.Button;
                st.LastDragTile = tile;
            }
            else
            {
                st.DragButton = e.Button; // still allow dragging onto the next tile
                st.LastDragTile = tile;
            }
            return;
        }

        // Network Wrench.
        if (HoldingWrench())
        {
            var layer = store.Find(location);
            var wire = layer?.Get(tile);
            if (layer == null || wire == null)
                return;

            if (action && wire.Kind == WireKind.Heavy)
            {
                helper.Input.Suppress(e.Button);
                layer.CycleColor(tile);
                Game1.playSound("drumkit6");
            }
            else if (useTool)
            {
                helper.Input.Suppress(e.Button);
                st.WrenchButton = e.Button;
                st.WrenchTile = tile;
                st.WrenchHeldTicks = 0;
                st.WrenchRunDone = false;
            }
            return;
        }

        // Pickaxe picks wire up from a bare tile (no object, no flooring on top).
        if (useTool && Game1.player.CurrentTool is Pickaxe)
        {
            var layer = store.Find(location);
            var wire = layer?.Get(tile);
            var v = new Vector2(tile.X, tile.Y);
            if (layer == null || wire == null || location.objects.ContainsKey(v) || location.terrainFeatures.ContainsKey(v))
                return;

            layer.Remove(tile);
            string id = wire.Kind == WireKind.Heavy ? Ids.HeavyCable : Ids.CopperWire;
            Game1.createItemDebris(ItemRegistry.Create("(O)" + id), new Vector2(tile.X * 64 + 32, tile.Y * 64 + 32), -1, location);
            Game1.playSound("hammer");
        }
    }

    private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
    {
        var st = state.Value;
        if (e.Button == st.DragButton)
        {
            st.DragButton = null;
            st.LastDragTile = null;
        }

        if (e.Button == st.WrenchButton)
        {
            st.WrenchButton = null;
            if (!st.WrenchRunDone && Context.IsWorldReady)
            {
                var covered = store.Find(Game1.currentLocation)?.ToggleCovered(st.WrenchTile);
                if (covered != null)
                    Game1.playSound(covered.Value ? "dirtyHit" : "pickUpItem");
            }
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;
        var st = state.Value;

        renderer.NetworkView = st.NetworkViewToggled || HoldingWrench();

        // Drag to lay a line.
        if (st.DragButton is { } drag && HeldWireKind() is { } kind && Context.IsPlayerFree)
        {
            if (!helper.Input.IsDown(drag))
            {
                st.DragButton = null;
            }
            else
            {
                var tile = WireTile(helper.Input.GetCursorPosition());
                if (tile != st.LastDragTile)
                {
                    st.LastDragTile = tile;
                    TryLay(Game1.currentLocation, tile, kind);
                }
            }
        }

        // Hold the wrench to change a whole run.
        if (st.WrenchButton is { } wrench && !st.WrenchRunDone)
        {
            if (!helper.Input.IsDown(wrench))
            {
                st.WrenchButton = null;
                return;
            }
            if (++st.WrenchHeldTicks >= config.WrenchHoldTicks)
            {
                st.WrenchRunDone = true;
                var layer = store.Find(Game1.currentLocation);
                var wire = layer?.Get(st.WrenchTile);
                if (layer != null && wire != null)
                {
                    int changed = layer.SetRunCovered(st.WrenchTile, !wire.Covered);
                    Game1.playSound("dirtyHit");
                    Game1.addHUDMessage(new HUDMessage(helper.Translation.Get("wire.run", new { count = changed })) { noIcon = true });
                }
            }
        }
    }

    /// <summary>Lays one wire from the held stack if the tile can take it.</summary>
    private bool TryLay(GameLocation location, TilePos tile, WireKind kind)
    {
        if (!CanLay(location, tile))
            return false;

        var layer = store.For(location);
        if (!layer.TryPlace(tile, kind))
            return false;

        Game1.player.reduceActiveItemByOne();
        Game1.playSound("woodyStep");
        return true;
    }

    private static bool CanLay(GameLocation location, TilePos tile)
    {
        var v = new Vector2(tile.X, tile.Y);
        if (!location.isTileOnMap(v) || location.isWaterTile(tile.X, tile.Y))
            return false;

        // A chest or machine on the tile is fine: that's where it connects.
        if (location.objects.ContainsKey(v))
            return true;

        return location.isTilePassable(v);
    }
}
