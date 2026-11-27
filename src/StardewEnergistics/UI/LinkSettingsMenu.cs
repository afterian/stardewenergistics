using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewEnergistics.Core.Machines;
using StardewEnergistics.Core.Storage;
using StardewEnergistics.Storage;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace StardewEnergistics.UI;

/// <summary>
/// Settings for a Machine Link or Shipping Link: inputs in priority order (click inventory items to add),
/// keep-back amount per input, output mode, schedule, copy/paste, and removing the Link.
/// A Link counts as set up once it has at least one input.
/// </summary>
internal sealed class LinkSettingsMenu : IClickableMenu
{
    private const int MaxInputs = 8;
    private const int RowHeight = 56;
    private static readonly PerScreen<LinkSettings?> Clipboard = new();

    private readonly ITranslationHelper i18n;
    private readonly Func<string, Texture2D> sprite;
    private readonly SObject target;
    private readonly bool shipping;
    private readonly string title;
    private readonly Action<SObject> onRemove;
    private readonly Action<SObject, LinkSettings> onSave;
    private readonly InventoryMenu inventory;

    private List<InputRule> inputs;
    private Dictionary<string, long> keep;
    private OutputMode output;
    private Schedule schedule;

    private readonly List<(ClickableComponent Button, Action<bool> Click, string? Label, int Icon)> buttons = new();
    private Item? hoverItem;

    public LinkSettingsMenu(ITranslationHelper i18n, Func<string, Texture2D> sprite, SObject target, bool shipping, string title,
        LinkSettings settings, Action<SObject, LinkSettings> onSave, Action<SObject> onRemove)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        this.i18n = i18n;
        this.sprite = sprite;
        this.target = target;
        this.shipping = shipping;
        this.title = title;
        this.onSave = onSave;
        this.onRemove = onRemove;

        inputs = settings.Inputs.ToList();
        keep = settings.KeepBack.ToDictionary(k => k.Item.QualifiedId, k => k.Amount);
        output = settings.Output;
        schedule = settings.Schedule;

        width = 12 * 64 + borderWidth * 2;
        height = 120 + MaxInputs * RowHeight + 80 + 3 * 64 + borderWidth * 2;
        var pos = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
        xPositionOnScreen = (int)pos.X;
        yPositionOnScreen = (int)pos.Y;
        initializeUpperRightCloseButton();

        inventory = new InventoryMenu(xPositionOnScreen + borderWidth, yPositionOnScreen + height - borderWidth - 3 * 64, playerInventory: true);
        Layout();
        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }

    private int Left => xPositionOnScreen + borderWidth;

    private int Top => yPositionOnScreen + borderWidth;

    private LinkSettings Current() => new(
        inputs,
        inputs.Where(i => keep.TryGetValue(i.Item.QualifiedId, out long n) && n > 0).Select(i => new KeepBackRule(new ItemKey(i.Item.QualifiedId), keep[i.Item.QualifiedId])).ToList(),
        output,
        schedule,
        inputs.Count > 0);

    private void Add(Rectangle r, Action<bool> click, string? label = null, int icon = -1) =>
        buttons.Add((new ClickableComponent(r, label ?? icon.ToString())
        {
            myID = 7000 + buttons.Count,
            leftNeighborID = ClickableComponent.SNAP_AUTOMATIC,
            rightNeighborID = ClickableComponent.SNAP_AUTOMATIC,
            upNeighborID = ClickableComponent.SNAP_AUTOMATIC,
            downNeighborID = ClickableComponent.SNAP_AUTOMATIC,
        }, click, label, icon));

    /// <summary>Rebuilds buttons after any change.</summary>
    private void Layout()
    {
        buttons.Clear();
        int y = Top + 96;
        for (int i = 0; i < inputs.Count; i++)
        {
            int index = i;
            int rowY = y + i * RowHeight;
            int right = Left + 12 * 64;
            Add(new Rectangle(right - 520, rowY + 4, 36, 36), _ => Move(index, -1), icon: 0);
            Add(new Rectangle(right - 476, rowY + 4, 36, 36), _ => Move(index, 1), icon: 1);
            Add(new Rectangle(right - 432, rowY + 4, 36, 36), _ => { inputs.RemoveAt(index); Layout(); }, icon: 3);
            Add(new Rectangle(right - 360, rowY, 56, 44), big => Keep(index, big ? -10 : -1), "-");
            Add(new Rectangle(right - 96, rowY, 56, 44), big => Keep(index, big ? 10 : 1), "+");
        }

        int bottom = Top + 96 + MaxInputs * RowHeight + 8;
        int x = Left;
        if (!shipping)
        {
            Add(new Rectangle(x, bottom, 300, 52), _ => { output = output == OutputMode.ToStorage ? OutputMode.LeaveInMachine : OutputMode.ToStorage; }, "output");
            x += 312;
        }
        Add(new Rectangle(x, bottom, 220, 52), _ => { schedule = schedule with { Always = !schedule.Always }; Layout(); }, "schedule");
        x += 232;
        if (!schedule.Always)
        {
            Add(new Rectangle(x, bottom, 44, 52), big => Hours(true, -1), "-");
            Add(new Rectangle(x + 48, bottom, 44, 52), big => Hours(true, 1), "+");
            Add(new Rectangle(x + 100, bottom, 44, 52), big => Hours(false, -1), "-");
            Add(new Rectangle(x + 148, bottom, 44, 52), big => Hours(false, 1), "+");
        }

        int right2 = Left + 12 * 64;
        Add(new Rectangle(right2 - 320, Top + 8, 100, 52), _ => Copy(), i18n.Get("link.copy"));
        Add(new Rectangle(right2 - 212, Top + 8, 100, 52), _ => Paste(), i18n.Get("link.paste"));
        Add(new Rectangle(right2 - 104, Top + 8, 52, 52), _ => Remove(), icon: 3);

        allClickableComponents = buttons.Select(b => b.Button).ToList();
        allClickableComponents.AddRange(inventory.inventory);
        if (upperRightCloseButton != null)
            allClickableComponents.Add(upperRightCloseButton);
    }

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = inventory.inventory.FirstOrDefault();
        snapCursorToCurrentSnappedComponent();
    }

    private void Move(int index, int delta)
    {
        int to = index + delta;
        if (to < 0 || to >= inputs.Count)
            return;
        (inputs[index], inputs[to]) = (inputs[to], inputs[index]);
        Game1.playSound("shwip");
        Layout();
    }

    private void Keep(int index, int delta)
    {
        string id = inputs[index].Item.QualifiedId;
        long n = Math.Max(0, (keep.TryGetValue(id, out long k) ? k : 0) + delta);
        keep[id] = n;
        Game1.playSound("smallSelect");
    }

    private void Hours(bool from, int delta)
    {
        int Step(int t) => Math.Clamp(t + delta * 100, 600, 2600);
        schedule = from ? schedule with { FromTime = Step(schedule.FromTime) } : schedule with { ToTime = Step(schedule.ToTime) };
        Game1.playSound("smallSelect");
    }

    private void Copy()
    {
        Clipboard.Value = Current();
        Game1.playSound("bigSelect");
        Game1.addHUDMessage(new HUDMessage(i18n.Get("link.copied")) { noIcon = true });
    }

    private void Paste()
    {
        if (Clipboard.Value is not { } s)
            return;
        inputs = s.Inputs.ToList();
        keep = s.KeepBack.ToDictionary(k => k.Item.QualifiedId, k => k.Amount);
        output = s.Output;
        schedule = s.Schedule;
        Game1.playSound("bigSelect");
        Game1.addHUDMessage(new HUDMessage(i18n.Get("link.pasted")) { noIcon = true });
        Layout();
    }

    private bool removed;

    private void Remove()
    {
        removed = true;
        onRemove(target);
        exitThisMenu();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true) => Click(x, y, big: false);

    public override void receiveRightClick(int x, int y, bool playSound = true) => Click(x, y, big: true);

    private void Click(int x, int y, bool big)
    {
        if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y))
        {
            exitThisMenu();
            return;
        }

        foreach (var (button, click, _, _) in buttons.ToList())
        {
            if (button.containsPoint(x, y))
            {
                click(big || Game1.oldKBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift));
                return;
            }
        }

        var item = inventory.getItemAt(x, y);
        if (item != null && inputs.Count < MaxInputs)
        {
            if (!StardewItemKeys.TryGetKey(item, out var key))
                key = new ItemKey(item.QualifiedItemId, item.Quality);
            if (inputs.All(i => i.Item.QualifiedId != key.QualifiedId))
            {
                inputs.Add(new InputRule(new ItemKey(key.QualifiedId), AnyQuality: true));
                Game1.playSound("coin");
                Layout();
            }
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        hoverItem = inventory.hover(x, y, null);
    }

    protected override void cleanupBeforeExit()
    {
        if (!removed)
            onSave(target, Current());
        base.cleanupBeforeExit();
    }

    private static string Time(int t) => Game1.getTimeOfDayString(t >= 2400 ? t - 2400 : t);

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds)
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        Utility.drawTextWithShadow(b, title, Game1.dialogueFont, new Vector2(Left, Top), Game1.textColor);
        b.DrawString(Game1.smallFont, inputs.Count == 0 ? i18n.Get("link.none") : i18n.Get(shipping ? "link.ship" : "link.inputs"), new Vector2(Left, Top + 60), inputs.Count == 0 ? Color.DarkRed : Game1.textColor);

        int y = Top + 96;
        for (int i = 0; i < inputs.Count; i++)
        {
            int rowY = y + i * RowHeight;
            var item = StardewItemKeys.Create(inputs[i].Item, 1);
            item.drawInMenu(b, new Vector2(Left, rowY - 6), 0.8f, 1f, 0.9f, StackDrawType.Hide);
            b.DrawString(Game1.smallFont, $"{i + 1}. {item.DisplayName}", new Vector2(Left + 64, rowY + 10), Game1.textColor);
            long n = keep.TryGetValue(inputs[i].Item.QualifiedId, out long k) ? k : 0;
            string keepText = i18n.Get("link.keep", new { n });
            b.DrawString(Game1.smallFont, keepText, new Vector2(Left + 12 * 64 - 296, rowY + 10), Game1.textColor);
        }

        foreach (var (button, _, label, icon) in buttons)
        {
            var r = button.bounds;
            if (icon >= 0)
            {
                b.Draw(sprite("settings-icons.png"), new Vector2(r.X, r.Y), new Rectangle(icon * 9, 0, 9, 9), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.9f);
                continue;
            }
            drawTextureBox(b, r.X, r.Y, r.Width, r.Height, Color.White);
            string text = label switch
            {
                "output" => i18n.Get(output == OutputMode.ToStorage ? "link.output.storage" : "link.output.leave"),
                "schedule" => schedule.Always ? i18n.Get("link.schedule.always") : i18n.Get("link.schedule.hours", new { from = Time(schedule.FromTime), to = Time(schedule.ToTime) }),
                _ => label ?? "",
            };
            var size = Game1.smallFont.MeasureString(text);
            float scale = Math.Min(1f, (r.Width - 12) / Math.Max(1f, size.X));
            b.DrawString(Game1.smallFont, text, new Vector2(r.Center.X - size.X * scale / 2, r.Center.Y - size.Y * scale / 2), Game1.textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }

        inventory.draw(b);
        base.draw(b);
        if (hoverItem != null)
            drawToolTip(b, hoverItem.getDescription(), hoverItem.DisplayName, hoverItem);
        drawMouse(b);
    }
}
