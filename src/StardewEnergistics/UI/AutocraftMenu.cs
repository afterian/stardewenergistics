using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewEnergistics.Core.Crafting;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Crafting;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace StardewEnergistics.UI;

/// <summary>What the autocraft screen needs from the network (host and split-screen only).</summary>
internal sealed record AutocraftBackend(
    Func<IReadOnlyList<ItemKey>> Craftable,
    Func<ItemKey, int, CraftPlan?> Plan,
    Func<ItemKey, int, string?> Start,
    Func<IReadOnlyList<CraftJob>> Jobs,
    Func<int, bool> Cancel,
    bool Advanced,
    Func<IReadOnlyList<StockRule>> StockRules,
    Func<ItemKey, int, string?> SetStockRule);

/// <summary>
/// Autocraft screen: pick a known recipe's item, choose how many, see what it uses or what's missing,
/// and start a job. Below, the network's jobs with progress and Cancel.
/// </summary>
internal sealed class AutocraftMenu : IClickableMenu
{
    private const int Cols = 12, GridRows = 3, Slot = 64, JobRows = 4, JobRowHeight = 44;
    private const int IdBase = 8000;

    private readonly AutocraftBackend backend;
    private readonly ITranslationHelper i18n;
    private readonly TextBox search;
    private readonly Rectangle searchBox;
    private readonly List<ClickableComponent> slots = new();
    private readonly ClickableComponent minus, plus, start, keepButton;
    private readonly List<ClickableComponent> cancelButtons = new();

    private readonly IReadOnlyList<ItemKey> all;
    private List<(ItemKey Key, Item Item)> shown = new();
    private string lastSearch = "";
    private int scrollRow;
    private ItemKey? selected;
    private int amount = 1;
    private CraftPlan? plan;
    private IReadOnlyList<CraftJob> jobs = Array.Empty<CraftJob>();
    private Item? hover;
    private int refreshTicks;

    public AutocraftMenu(AutocraftBackend backend, ITranslationHelper i18n)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        this.backend = backend;
        this.i18n = i18n;

        width = Cols * Slot + borderWidth * 2;
        height = 112 + GridRows * Slot + 132 + 40 + JobRows * JobRowHeight + borderWidth * 2;
        var pos = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
        xPositionOnScreen = (int)pos.X;
        yPositionOnScreen = (int)pos.Y;
        initializeUpperRightCloseButton();

        searchBox = new Rectangle(Left, Top + 56, 320, 48);
        search = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor)
        {
            X = searchBox.X,
            Y = searchBox.Y,
            Width = searchBox.Width,
            Text = "",
        };

        for (int i = 0; i < Cols * GridRows; i++)
        {
            int c = i % Cols, r = i / Cols;
            slots.Add(new ClickableComponent(new Rectangle(Left + c * Slot, GridTop + r * Slot, Slot, Slot), i.ToString())
            {
                myID = IdBase + i,
                leftNeighborID = c > 0 ? IdBase + i - 1 : -1,
                rightNeighborID = c < Cols - 1 ? IdBase + i + 1 : -1,
                upNeighborID = r > 0 ? IdBase + i - Cols : -1,
                downNeighborID = r < GridRows - 1 ? IdBase + i + Cols : IdBase + 100,
            });
        }

        int detailTop = GridTop + GridRows * Slot + 8;
        int right = Left + Cols * Slot;
        minus = Button(new Rectangle(right - 360, detailTop, 56, 48), "-", IdBase + 100);
        plus = Button(new Rectangle(right - 200, detailTop, 56, 48), "+", IdBase + 101);
        start = Button(new Rectangle(right - 136, detailTop, 136, 48), "start", IdBase + 102);
        keepButton = Button(new Rectangle(right - 520, detailTop, 150, 48), "keep", IdBase + 103);

        for (int i = 0; i < JobRows; i++)
            cancelButtons.Add(Button(new Rectangle(right - 136, JobsTop + i * JobRowHeight, 136, 40), "cancel", IdBase + 200 + i));

        allClickableComponents = new List<ClickableComponent>(slots) { minus, plus, start };
        if (backend.Advanced)
            allClickableComponents.Add(keepButton);
        allClickableComponents.AddRange(cancelButtons);
        if (upperRightCloseButton != null)
            allClickableComponents.Add(upperRightCloseButton);

        all = backend.Craftable();
        Filter();
        RefreshJobs();
        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }

    private static ClickableComponent Button(Rectangle r, string name, int id) => new(r, name)
    {
        myID = id,
        leftNeighborID = ClickableComponent.SNAP_AUTOMATIC,
        rightNeighborID = ClickableComponent.SNAP_AUTOMATIC,
        upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
        downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
    };

    private int Left => xPositionOnScreen + borderWidth;
    private int Top => yPositionOnScreen + borderWidth;
    private int GridTop => Top + 112;
    private int JobsTop => GridTop + GridRows * Slot + 132 + 40;

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = slots.FirstOrDefault();
        snapCursorToCurrentSnappedComponent();
    }

    private void Filter()
    {
        string q = search.Text.Trim();
        lastSearch = search.Text;
        shown = all
            .Select(k => (Key: k, Item: ItemRegistry.Create(k.QualifiedId, 1, allowNull: true)))
            .Where(p => p.Item != null && (q.Length == 0 || p.Item.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .Select(p => (p.Key, p.Item!))
            .OrderBy(p => p.Item2.DisplayName)
            .ToList();
        scrollRow = 0;
    }

    private IReadOnlyList<StockRule> stockRules = Array.Empty<StockRule>();

    private void RefreshJobs()
    {
        jobs = backend.Jobs().Where(j => !j.Finished).Take(JobRows).ToList();
        if (backend.Advanced)
            stockRules = backend.StockRules();
    }

    private void Replan() => plan = selected is { } k ? backend.Plan(k, amount) : null;

    public override void update(GameTime time)
    {
        base.update(time);
        if (search.Text != lastSearch)
            Filter();
        if (++refreshTicks >= 60)
        {
            refreshTicks = 0;
            RefreshJobs();
        }
    }

    public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
    {
        // Typing in the search box shouldn't close the menu.
        if (search.Selected && key != Microsoft.Xna.Framework.Input.Keys.Escape)
            return;
        base.receiveKeyPress(key);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        int maxRow = Math.Max(0, (shown.Count + Cols - 1) / Cols - GridRows);
        scrollRow = Math.Clamp(scrollRow + (direction > 0 ? -1 : 1), 0, maxRow);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true) => Click(x, y, big: false);

    public override void receiveRightClick(int x, int y, bool playSound = true) => Click(x, y, big: true);

    private void Click(int x, int y, bool big)
    {
        big |= Game1.oldKBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);
        if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y))
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

        for (int i = 0; i < slots.Count; i++)
        {
            int index = scrollRow * Cols + i;
            if (slots[i].containsPoint(x, y) && index < shown.Count)
            {
                selected = shown[index].Key;
                amount = 1;
                Game1.playSound("smallSelect");
                Replan();
                return;
            }
        }

        if (selected != null && minus.containsPoint(x, y))
        {
            amount = Math.Max(1, amount - (big ? 10 : 1));
            Game1.playSound("smallSelect");
            Replan();
            return;
        }
        if (selected != null && plus.containsPoint(x, y))
        {
            amount = Math.Min(999, amount + (big ? 10 : 1));
            Game1.playSound("smallSelect");
            Replan();
            return;
        }
        if (selected is { } key && start.containsPoint(x, y))
        {
            string? error = backend.Start(key, amount);
            if (error != null)
            {
                Game1.showRedMessage(error);
                return;
            }
            Game1.playSound("bigSelect");
            Game1.addHUDMessage(new HUDMessage(i18n.Get("auto.started", new { n = amount, item = CraftingService.NameOf(key.QualifiedId) })) { noIcon = true });
            RefreshJobs();
            Replan();
            return;
        }

        if (backend.Advanced && selected is { } keepKey && keepButton.containsPoint(x, y))
        {
            var rules = backend.StockRules();
            bool clear = rules.Any(r => r.Item.QualifiedId == keepKey.QualifiedId && r.Target == amount);
            string? error = backend.SetStockRule(keepKey, clear ? 0 : amount);
            if (error != null)
            {
                Game1.showRedMessage(error);
                return;
            }
            Game1.playSound("coin");
            RefreshJobs();
            string item = CraftingService.NameOf(keepKey.QualifiedId);
            Game1.addHUDMessage(new HUDMessage(clear ? i18n.Get("auto.keep.cleared", new { item }) : i18n.Get("auto.keep.set", new { n = amount, item })) { noIcon = true });
            return;
        }

        for (int i = 0; i < cancelButtons.Count && i < jobs.Count; i++)
        {
            if (cancelButtons[i].containsPoint(x, y))
            {
                if (backend.Cancel(jobs[i].Id))
                {
                    Game1.playSound("trashcan");
                    Game1.addHUDMessage(new HUDMessage(i18n.Get("auto.cancelled")) { noIcon = true });
                }
                RefreshJobs();
                Replan();
                return;
            }
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        hover = null;
        for (int i = 0; i < slots.Count; i++)
        {
            int index = scrollRow * Cols + i;
            if (slots[i].containsPoint(x, y) && index < shown.Count)
                hover = shown[index].Item;
        }
    }

    private string JobText(CraftJob j)
    {
        string item = CraftingService.NameOf(j.Target.Item.QualifiedId);
        if (j.Status == JobStatus.Queued)
            return i18n.Get("auto.queued", new { n = j.Target.Count, item });
        return i18n.Get("auto.job", new { n = j.Target.Count, item, pct = (int)(j.Progress * 100) });
    }

    private void DrawButton(SpriteBatch b, ClickableComponent c, string text, bool enabled)
    {
        var r = c.bounds;
        drawTextureBox(b, r.X, r.Y, r.Width, r.Height, enabled ? Color.White : Color.Gray);
        var size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(r.Center.X - size.X / 2, r.Center.Y - size.Y / 2), Game1.textColor);
    }

    private static void DrawWrapped(SpriteBatch b, string text, Vector2 at, int width, int maxLines, Color color)
    {
        string wrapped = Game1.parseText(text, Game1.smallFont, width);
        var lines = wrapped.Split('\n').Take(maxLines);
        b.DrawString(Game1.smallFont, string.Join("\n", lines), at, color);
    }

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds)
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        Utility.drawTextWithShadow(b, i18n.Get("auto.title"), Game1.dialogueFont, new Vector2(Left, Top - 8), Game1.textColor);
        search.Draw(b);
        if (search.Text.Length == 0 && !search.Selected)
            b.DrawString(Game1.smallFont, i18n.Get("auto.search"), new Vector2(searchBox.X + 16, searchBox.Y + 10), Game1.textColor * 0.5f);

        // Recipe grid.
        for (int i = 0; i < slots.Count; i++)
        {
            var r = slots[i].bounds;
            int index = scrollRow * Cols + i;
            bool isSelected = index < shown.Count && selected == shown[index].Key;
            b.Draw(Game1.menuTexture, r, Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, isSelected ? 56 : 10), Color.White);
            if (index < shown.Count)
                shown[index].Item.drawInMenu(b, new Vector2(r.X, r.Y), 1f, 1f, 0.9f, StackDrawType.Hide);
        }

        // Selected recipe.
        int detailTop = GridTop + GridRows * Slot + 8;
        if (selected is { } key)
        {
            string name = CraftingService.NameOf(key.QualifiedId);
            b.DrawString(Game1.smallFont, name, new Vector2(Left, detailTop + 10), Game1.textColor);
            DrawButton(b, minus, "-", true);
            string amountText = i18n.Get("auto.amount", new { n = amount });
            var size = Game1.smallFont.MeasureString(amountText);
            b.DrawString(Game1.smallFont, amountText, new Vector2((minus.bounds.Right + plus.bounds.X) / 2f - size.X / 2, detailTop + 10), Game1.textColor);
            DrawButton(b, plus, "+", true);
            DrawButton(b, start, i18n.Get("auto.start"), plan?.CanRun == true);
            if (backend.Advanced)
                DrawButton(b, keepButton, i18n.Get("auto.keep", new { n = amount }), true);

            if (plan != null)
            {
                string info = plan.CanRun
                    ? i18n.Get("auto.ready", new { items = CraftingService.Describe(plan.FromStock.GroupBy(a => a.Item.QualifiedId).Select(g => new ItemAmount(new ItemKey(g.Key), g.Sum(a => a.Count)))) })
                    : i18n.Get("auto.missing", new { items = CraftingService.Describe(plan.Missing) });
                DrawWrapped(b, info, new Vector2(Left, detailTop + 60), Cols * Slot, 3, plan.CanRun ? Game1.textColor : Color.DarkRed);
            }
        }

        // Jobs.
        Utility.drawTextWithShadow(b, i18n.Get("auto.jobs"), Game1.smallFont, new Vector2(Left, JobsTop - 36), Game1.textColor);
        if (backend.Advanced && stockRules.Count > 0)
        {
            string kept = string.Join(", ", stockRules.Select(r => $"{r.Target} {CraftingService.NameOf(r.Item.QualifiedId)}"));
            DrawWrapped(b, i18n.Get("auto.keep.rules", new { items = kept }), new Vector2(Left + 120, JobsTop - 36), Cols * Slot - 120, 1, Game1.textColor * 0.8f);
        }
        if (jobs.Count == 0)
            b.DrawString(Game1.smallFont, i18n.Get("auto.noJobs"), new Vector2(Left, JobsTop + 8), Game1.textColor * 0.6f);
        for (int i = 0; i < jobs.Count; i++)
        {
            int y = JobsTop + i * JobRowHeight;
            ItemRegistry.Create(jobs[i].Target.Item.QualifiedId, 1, allowNull: true)?.drawInMenu(b, new Vector2(Left - 8, y - 12), 0.6f, 1f, 0.9f, StackDrawType.Hide);
            b.DrawString(Game1.smallFont, JobText(jobs[i]), new Vector2(Left + 44, y + 8), Game1.textColor);
            DrawButton(b, cancelButtons[i], i18n.Get("auto.cancel"), true);
        }

        base.draw(b);
        if (hover != null)
            drawToolTip(b, hover.getDescription(), hover.DisplayName, hover);
        drawMouse(b);
    }
}
