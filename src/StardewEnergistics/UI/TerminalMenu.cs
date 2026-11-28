using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewEnergistics.Core.Storage;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace StardewEnergistics.UI;

/// <summary>
/// The Network Terminal's Storage screen: a scrolling, searchable grid of everything on the network,
/// the player's inventory below, power and disk meters, and a Craft button.
/// Left-click takes a stack (right-click takes one); clicking your own items stores them.
/// Controller: move between slots, A takes/stores, triggers scroll.
/// </summary>
internal sealed class TerminalMenu : IClickableMenu
{
    private const int Cols = 12;
    private const int Rows = 4;
    private const int Slot = 64;
    private const int HeaderHeight = 136;
    private const int GridIdBase = 5000;

    private readonly ITerminalBackend backend;
    private readonly ITranslationHelper i18n;
    private readonly Func<string, Texture2D> sprite;
    private readonly Action? onCraft;
    private readonly Action? onAutocraft;

    private readonly TextBox search;
    private readonly InventoryMenu inventory;
    private readonly List<ClickableComponent> gridSlots = new();
    private readonly ClickableComponent craftButton;
    private readonly ClickableComponent autoButton;
    private readonly Rectangle searchBox;

    private IReadOnlyList<TerminalRow> rows = Array.Empty<TerminalRow>();
    private List<TerminalRow> shown = new();
    private TerminalStatus? status;
    private int scrollRow;
    private bool busy;
    private string lastSearch = "";
    private TerminalRow? hoverRow;
    private Item? hoverInventory;

    public TerminalMenu(ITerminalBackend backend, ITranslationHelper i18n, Func<string, Texture2D> sprite, Action? onCraft, Action? onAutocraft = null)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        this.backend = backend;
        this.i18n = i18n;
        this.sprite = sprite;
        this.onCraft = backend.CanCraft ? onCraft : null;
        this.onAutocraft = backend.CanCraft ? onAutocraft : null;

        width = Cols * Slot + borderWidth * 2;
        height = HeaderHeight + Rows * Slot + 48 + 3 * Slot + borderWidth * 2;
        var pos = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
        xPositionOnScreen = (int)pos.X;
        yPositionOnScreen = (int)pos.Y;
        initializeUpperRightCloseButton();

        int left = xPositionOnScreen + borderWidth;
        int top = yPositionOnScreen + borderWidth;

        searchBox = new Rectangle(left, top + 56, 320, 48);
        search = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
            X = searchBox.X,
            Y = searchBox.Y,
            Width = searchBox.Width,
            Text = "",
        };

        craftButton = new ClickableComponent(new Rectangle(left + Cols * Slot - 160, top + 56, 160, 48), "craft")
        {
            myID = GridIdBase - 1,
            downNeighborID = GridIdBase + Cols - 1,
            leftNeighborID = GridIdBase - 2,
        };
        autoButton = new ClickableComponent(new Rectangle(craftButton.bounds.X - 16 - 180, top + 56, 180, 48), "autocraft")
        {
            myID = GridIdBase - 2,
            downNeighborID = GridIdBase + Cols - 4,
            rightNeighborID = GridIdBase - 1,
        };

        int gridTop = top + HeaderHeight;
        for (int i = 0; i < Cols * Rows; i++)
        {
            int c = i % Cols, r = i / Cols;
            gridSlots.Add(new ClickableComponent(new Rectangle(left + c * Slot, gridTop + r * Slot, Slot, Slot), i.ToString())
            {
                myID = GridIdBase + i,
                leftNeighborID = c > 0 ? GridIdBase + i - 1 : -1,
                rightNeighborID = c < Cols - 1 ? GridIdBase + i + 1 : -1,
                upNeighborID = r > 0 ? GridIdBase + i - Cols : craftButton.myID,
                downNeighborID = r < Rows - 1 ? GridIdBase + i + Cols : c,
            });
        }

        inventory = new InventoryMenu(left, gridTop + Rows * Slot + 48, playerInventory: true);
        for (int c = 0; c < Cols && c < inventory.inventory.Count; c++)
            inventory.inventory[c].upNeighborID = GridIdBase + (Rows - 1) * Cols + c;

        allClickableComponents = new List<ClickableComponent>(gridSlots) { craftButton, autoButton };
        allClickableComponents.AddRange(inventory.inventory);
        if (upperRightCloseButton != null)
            allClickableComponents.Add(upperRightCloseButton);

        Reload();
        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = getComponentWithID(GridIdBase);
        snapCursorToCurrentSnappedComponent();
    }

    private void Reload()
    {
        busy = true;
        backend.Refresh((r, s) =>
        {
            rows = r;
            status = s;
            busy = false;
            ApplyFilter();
        });
    }

    private void ApplyFilter()
    {
        string q = search.Text.Trim();
        shown = rows
            .Where(r => q.Length == 0 || r.Display.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Display.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(r => r.Display.Quality)
            .ToList();
        int maxScroll = Math.Max(0, (shown.Count + Cols - 1) / Cols - Rows);
        scrollRow = Math.Clamp(scrollRow, 0, maxScroll);
        lastSearch = search.Text;
    }

    private TerminalRow? RowAt(int x, int y)
    {
        for (int i = 0; i < gridSlots.Count; i++)
        {
            if (!gridSlots[i].containsPoint(x, y))
                continue;
            int index = scrollRow * Cols + i;
            return index < shown.Count ? shown[index] : null;
        }
        return null;
    }

    private void Scroll(int delta)
    {
        int maxScroll = Math.Max(0, (shown.Count + Cols - 1) / Cols - Rows);
        int next = Math.Clamp(scrollRow + delta, 0, maxScroll);
        if (next != scrollRow)
        {
            scrollRow = next;
            Game1.playSound("shiny4");
        }
    }

    public override void receiveScrollWheelAction(int direction) => Scroll(direction > 0 ? -1 : 1);

    public override void receiveGamePadButton(Buttons b)
    {
        if (b == Buttons.LeftTrigger)
            Scroll(-1);
        else if (b == Buttons.RightTrigger)
            Scroll(1);
        else
            base.receiveGamePadButton(b);
    }

    public override void receiveKeyPress(Keys key)
    {
        // While typing a search, only Escape closes the menu.
        if (search.Selected)
        {
            if (key == Keys.Escape)
                search.Selected = false;
            return;
        }
        base.receiveKeyPress(key);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true) => Click(x, y, one: false);

    public override void receiveRightClick(int x, int y, bool playSound = true) => Click(x, y, one: true);

    private void Click(int x, int y, bool one)
    {
        if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y) && !one)
        {
            exitThisMenu();
            return;
        }

        search.Selected = searchBox.Contains(x, y);
        if (search.Selected)
        {
            Game1.keyboardDispatcher.Subscriber = search;
            return;
        }

        if (busy)
            return;

        if (autoButton.containsPoint(x, y) && !one)
        {
            if (onAutocraft != null)
            {
                Game1.playSound("bigSelect");
                onAutocraft();
            }
            else
            {
                Game1.showRedMessage(i18n.Get("auto.hostOnly"));
            }
            return;
        }

        if (craftButton.containsPoint(x, y) && !one)
        {
            if (onCraft != null)
            {
                Game1.playSound("bigSelect");
                onCraft();
            }
            else
            {
                Game1.showRedMessage(i18n.Get("terminal.remoteCraft"));
            }
            return;
        }

        if (RowAt(x, y) is { } row)
        {
            int max = Math.Max(1, row.Display.maximumStackSize());
            int count = one ? 1 : (int)Math.Min(row.Count, max);
            busy = true;
            Game1.playSound("coin");
            backend.Take(row, count, Reload);
            return;
        }

        var item = inventory.getItemAt(x, y);
        if (item != null)
        {
            Item toStore;
            if (one && item.Stack > 1)
            {
                toStore = item.getOne();
                item.Stack--;
            }
            else
            {
                toStore = item;
                Game1.player.removeItemFromInventory(item);
            }

            busy = true;
            Game1.playSound("Ship");
            backend.Put(toStore, leftover =>
            {
                if (leftover != null)
                {
                    TerminalItems.Give(leftover);
                    Game1.showRedMessage(i18n.Get(WhyNotStored(leftover)));
                }
                Reload();
            });
        }
    }

    /// <summary>Why an item didn't go into storage, so the player knows what to fix.</summary>
    private string WhyNotStored(Item item)
    {
        bool storable = StardewEnergistics.Storage.StardewItemKeys.TryGetKey(item, out _);
        // Local rows carry a StoredRow, farmhand rows an ItemKey.
        bool kindStored = rows.Any(r => r.Handle switch
        {
            StardewEnergistics.Storage.StoredRow { Key: { } k } => k.QualifiedId == item.QualifiedItemId,
            ItemKey k => k.QualifiedId == item.QualifiedItemId,
            _ => false,
        });
        return StoreRefusals.Why(storable, status?.Disks ?? 1, status?.KindsUsed ?? 0, status?.KindsMax ?? 0, kindStored) switch
        {
            StoreRefusal.CantStore => "terminal.cantStore",
            StoreRefusal.NoDisks => "terminal.noDisks",
            StoreRefusal.KindsFull => "terminal.kindsFull",
            _ => "terminal.full",
        };
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        hoverRow = RowAt(x, y);
        hoverInventory = inventory.hover(x, y, null);
    }

    public override void update(GameTime time)
    {
        base.update(time);
        if (search.Text != lastSearch)
            ApplyFilter();
    }

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds)
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);

        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        int left = xPositionOnScreen + borderWidth;
        int top = yPositionOnScreen + borderWidth;

        // Title and meters.
        DrawTabIcon(b, TabStorage, left, top - 4, 3f);
        Utility.drawTextWithShadow(b, i18n.Get("terminal.title"), Game1.dialogueFont, new Vector2(left + 56, top - 8), Game1.textColor);
        DrawMeters(b, left + 360, top);

        // Search box and Craft button.
        search.Draw(b);
        if (search.Text.Length == 0 && !search.Selected)
            b.DrawString(Game1.smallFont, i18n.Get("terminal.search"), new Vector2(searchBox.X + 16, searchBox.Y + 10), Game1.textColor * 0.5f);
        drawTextureBox(b, craftButton.bounds.X, craftButton.bounds.Y, craftButton.bounds.Width, craftButton.bounds.Height, onCraft != null ? Color.White : Color.Gray);
        string craft = i18n.Get("terminal.tab.craft");
        var size = Game1.smallFont.MeasureString(craft);
        DrawTabIcon(b, TabCraft, craftButton.bounds.X + 12, craftButton.bounds.Center.Y - 16, 2f, onCraft != null);
        b.DrawString(Game1.smallFont, craft, new Vector2(craftButton.bounds.Center.X + 18 - size.X / 2, craftButton.bounds.Center.Y - size.Y / 2), Game1.textColor);
        drawTextureBox(b, autoButton.bounds.X, autoButton.bounds.Y, autoButton.bounds.Width, autoButton.bounds.Height, onAutocraft != null ? Color.White : Color.Gray);
        string auto = i18n.Get("auto.button");
        size = Game1.smallFont.MeasureString(auto);
        DrawTabIcon(b, TabJobs, autoButton.bounds.X + 12, autoButton.bounds.Center.Y - 16, 2f, onAutocraft != null);
        b.DrawString(Game1.smallFont, auto, new Vector2(autoButton.bounds.Center.X + 18 - size.X / 2, autoButton.bounds.Center.Y - size.Y / 2), Game1.textColor);

        // Network grid on the green terminal screen.
        var first = gridSlots[0].bounds;
        var grid = new Rectangle(first.X, first.Y, Cols * Slot, Rows * Slot);
        DrawNineSlice(b, sprite("terminal-frame.png"), new Rectangle(grid.X - ScreenInset, grid.Y - ScreenInset, grid.Width + ScreenInset * 2, grid.Height + ScreenInset * 2));
        for (int i = 0; i < gridSlots.Count; i++)
        {
            var r = gridSlots[i].bounds;
            b.Draw(Game1.staminaRect, new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), ScreenSlot);
            int index = scrollRow * Cols + i;
            if (index >= shown.Count)
                continue;
            var row = shown[index];
            row.Display.drawInMenu(b, new Vector2(r.X, r.Y), 1f, 1f, 0.9f, StackDrawType.Hide, Color.White, true);
            if (row.Count > 1)
            {
                int n = (int)Math.Min(row.Count, int.MaxValue);
                float w = Utility.getWidthOfTinyDigitString(n, 3f);
                Utility.drawTinyDigits(n, b, new Vector2(r.Right - w - 4, r.Bottom - 22), 3f, 1f, Color.White);
            }
        }
        if (shown.Count == 0 && !busy)
            b.DrawString(Game1.smallFont, rows.Count == 0 ? i18n.Get("terminal.empty") : i18n.Get("terminal.noMatch"), new Vector2(left + 16, top + HeaderHeight + 16), ScreenText);

        // Scroll hint.
        int totalRows = (shown.Count + Cols - 1) / Cols;
        if (totalRows > Rows)
        {
            string page = $"{scrollRow + 1}-{Math.Min(totalRows, scrollRow + Rows)} / {totalRows}";
            b.DrawString(Game1.smallFont, page, new Vector2(left + Cols * Slot - Game1.smallFont.MeasureString(page).X, top + HeaderHeight + Rows * Slot + ScreenInset), Game1.textColor);
        }

        // Player inventory.
        inventory.draw(b);

        base.draw(b);

        if (hoverRow != null)
            drawToolTip(b, hoverRow.Display.getDescription(), $"{hoverRow.Display.DisplayName} x{hoverRow.Count:N0}", hoverRow.Display);
        else if (hoverInventory != null)
            drawToolTip(b, hoverInventory.getDescription(), hoverInventory.DisplayName, hoverInventory);

        drawMouse(b);
    }

    private void DrawMeters(SpriteBatch b, int x, int y)
    {
        if (status == null)
            return;

        string powerText = status.Powered
            ? i18n.Get("terminal.meter.power", new { power = status.PowerLeft, days = status.DaysLeft })
            : i18n.Get("network.unpowered");
        DrawMeter(b, "power-meter.png", x, y + 4, status.Powered ? Math.Clamp(status.DaysLeft / 14f, 0f, 1f) : 0f);
        b.DrawString(Game1.smallFont, powerText, new Vector2(x + 200, y), status.Powered ? Game1.textColor : Color.Red);

        float used = status.ItemsMax > 0 ? (float)status.ItemsUsed / status.ItemsMax : 0f;
        DrawMeter(b, "storage-meter.png", x, y + 44, used);
        string kinds = status.KindsMax > 0 ? $"{status.KindsUsed}/{status.KindsMax}" : status.KindsUsed.ToString();
        b.DrawString(Game1.smallFont, i18n.Get("terminal.meter.disks", new { disks = status.Disks, kinds, items = status.ItemsUsed.ToString("N0"), maxItems = status.ItemsMax.ToString("N0") }), new Vector2(x + 200, y + 40), Game1.textColor);
    }

    private const int TabStorage = 0, TabCraft = 1, TabJobs = 2;

    /// <summary>How far the terminal-frame border reaches outside the item grid (4 px at 4x scale, plus a gap).</summary>
    private const int ScreenInset = 20;

    private static readonly Color ScreenSlot = new(0, 0, 0, 40);
    private static readonly Color ScreenText = new(206, 230, 196);

    /// <summary>tab-icons.png: storage crate, craft hammer, jobs gear (16 x 16 each).</summary>
    private void DrawTabIcon(SpriteBatch b, int icon, int x, int y, float scale, bool enabled = true) =>
        b.Draw(sprite("tab-icons.png"), new Vector2(x, y), new Rectangle(icon * 16, 0, 16, 16), enabled ? Color.White : Color.Gray, 0f, Vector2.Zero, scale, SpriteEffects.None, 0.89f);

    /// <summary>Draws a 48 x 48 frame with fixed 16 px corners at 4x, stretching the edges and centre.</summary>
    private static void DrawNineSlice(SpriteBatch b, Texture2D tex, Rectangle dest)
    {
        const int c = 16, scale = 4, d = c * scale;
        int[] sx = { 0, c, tex.Width - c }, sw = { c, tex.Width - 2 * c, c };
        int[] sy = { 0, c, tex.Height - c }, sh = { c, tex.Height - 2 * c, c };
        int[] dx = { dest.X, dest.X + d, dest.Right - d }, dw = { d, dest.Width - 2 * d, d };
        int[] dy = { dest.Y, dest.Y + d, dest.Bottom - d }, dh = { d, dest.Height - 2 * d, d };
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                if (dw[col] > 0 && dh[row] > 0)
                    b.Draw(tex, new Rectangle(dx[col], dy[row], dw[col], dh[row]), new Rectangle(sx[col], sy[row], sw[col], sh[row]), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.87f);
            }
        }
    }

    /// <summary>Meter sprite: frame 0 = empty frame, frame 1 = fill (cut to length).</summary>
    private void DrawMeter(SpriteBatch b, string file, int x, int y, float fill)
    {
        var tex = sprite(file);
        b.Draw(tex, new Vector2(x, y), new Rectangle(0, 0, 48, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.88f);
        int w = (int)Math.Round(48 * Math.Clamp(fill, 0f, 1f));
        if (w > 0)
            b.Draw(tex, new Vector2(x, y), new Rectangle(48, 0, w, 8), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.89f);
    }
}
