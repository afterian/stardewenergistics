using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewEnergistics.Content;
using StardewEnergistics.Core.Guide;
using StardewEnergistics.Core.Progression;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace StardewEnergistics.Guide;

/// <summary>
/// The Tinkerer's Journal: chapters on the left, that chapter's goals in the middle (ticked when done),
/// and the chosen goal on the right: Maru's note, how to get and use each part, its live recipe,
/// and for a letter goal what Maru is waiting for.
/// Controller: the stick or d-pad moves between chapters and goals (the highlighted one is shown),
/// LB/RB change chapter, LT/RT scroll the page, B closes.
/// </summary>
internal sealed class GuideMenu : IClickableMenu
{
    private const int ChapterIdBase = 100;
    private const int QuestIdBase = 200;
    private const int HeaderHeight = 76;
    private const int FooterHeight = 36;
    private const int Gap = 16;

    private readonly ITranslationHelper i18n;
    private readonly GuideFacts facts;
    private readonly IReadOnlySet<string> done;
    private readonly bool gatesOn;
    private readonly object tokens;

    private readonly List<ClickableComponent> chapterRows = new();
    private readonly List<ClickableComponent> questRows = new();
    private readonly List<Line> lines = new();

    private int chapter;
    private int quest;
    private int scroll;

    private Rectangle chapterCol, questCol, detailCol;
    private int chapterRowHeight, questRowHeight;

    /// <summary>One line on the right-hand page.</summary>
    private sealed record Line(string Text, SpriteFont Font, Color Color, int Height, int Indent = 0, bool? Check = null, Item? Icon = null);

    public GuideMenu(ITranslationHelper i18n, GuideFacts facts, IReadOnlySet<string> done, bool gatesOn, object tokens)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        this.i18n = i18n;
        this.facts = facts;
        this.done = done;
        this.gatesOn = gatesOn;
        this.tokens = tokens;

        // Start on the next goal to do.
        var next = GuideProgress.Next(done);
        chapter = next?.Chapter ?? 0;
        quest = next == null ? 0 : Math.Max(0, Chapter.Quests.ToList().FindIndex(q => q.Id == next.Id));

        Layout();
        if (Game1.options.SnappyMenus)
            snapToDefaultClickableComponent();
    }

    private GuideChapter Chapter => GuideBook.Chapters[chapter];

    private GuideQuest? Quest => quest >= 0 && quest < Chapter.Quests.Count ? Chapter.Quests[quest] : null;

    private string T(string key, object? t = null) => i18n.Get(key, t ?? tokens);

    // ---- Layout ----

    private void Layout()
    {
        var view = Game1.uiViewport;
        width = Math.Min(1200, view.Width - 64);
        height = Math.Min(740, view.Height - 48);
        var pos = Utility.getTopLeftPositionForCenteringOnScreen(width, height);
        xPositionOnScreen = (int)pos.X;
        yPositionOnScreen = (int)pos.Y;
        initializeUpperRightCloseButton();

        int left = xPositionOnScreen + borderWidth;
        int top = yPositionOnScreen + borderWidth + HeaderHeight;
        int inner = width - borderWidth * 2;
        int colHeight = height - borderWidth * 2 - HeaderHeight - FooterHeight;
        int chapterW = (int)(inner * 0.24f);
        int questW = (int)(inner * 0.28f);
        chapterCol = new Rectangle(left, top, chapterW, colHeight);
        questCol = new Rectangle(chapterCol.Right + Gap, top, questW, colHeight);
        detailCol = new Rectangle(questCol.Right + Gap, top, inner - chapterW - questW - Gap * 2, colHeight);

        chapterRowHeight = Math.Min(72, colHeight / GuideBook.Chapters.Count);
        int maxQuests = GuideBook.Chapters.Max(c => c.Quests.Count);
        questRowHeight = Math.Min(64, colHeight / maxQuests);

        chapterRows.Clear();
        for (int i = 0; i < GuideBook.Chapters.Count; i++)
        {
            chapterRows.Add(new ClickableComponent(new Rectangle(chapterCol.X, chapterCol.Y + i * chapterRowHeight, chapterCol.Width, chapterRowHeight - 4), $"ch{i}")
            {
                myID = ChapterIdBase + i,
                upNeighborID = i > 0 ? ChapterIdBase + i - 1 : -1,
                downNeighborID = i < GuideBook.Chapters.Count - 1 ? ChapterIdBase + i + 1 : -1,
                rightNeighborID = QuestIdBase,
            });
        }
        BuildQuestRows();
        BuildLines();
    }

    private void BuildQuestRows()
    {
        questRows.Clear();
        for (int i = 0; i < Chapter.Quests.Count; i++)
        {
            questRows.Add(new ClickableComponent(new Rectangle(questCol.X, questCol.Y + i * questRowHeight, questCol.Width, questRowHeight - 4), $"q{i}")
            {
                myID = QuestIdBase + i,
                upNeighborID = i > 0 ? QuestIdBase + i - 1 : -1,
                downNeighborID = i < Chapter.Quests.Count - 1 ? QuestIdBase + i + 1 : -1,
                leftNeighborID = ChapterIdBase + chapter,
            });
        }
        allClickableComponents = new List<ClickableComponent>(chapterRows);
        allClickableComponents.AddRange(questRows);
        if (upperRightCloseButton != null)
            allClickableComponents.Add(upperRightCloseButton);
    }

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = getComponentWithID(QuestIdBase + quest) ?? getComponentWithID(ChapterIdBase + chapter);
        snapCursorToCurrentSnappedComponent();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Layout();
    }

    // ---- Selection ----

    private void SelectChapter(int index, bool snap)
    {
        index = (index + GuideBook.Chapters.Count) % GuideBook.Chapters.Count;
        if (index == chapter)
            return;
        chapter = index;
        int firstOpen = Chapter.Quests.ToList().FindIndex(q => !done.Contains(q.Id));
        quest = Math.Max(0, firstOpen);
        BuildQuestRows();
        BuildLines();
        Game1.playSound("shwip");
        if (snap && Game1.options.SnappyMenus)
        {
            currentlySnappedComponent = getComponentWithID(ChapterIdBase + chapter);
            snapCursorToCurrentSnappedComponent();
        }
    }

    private void SelectQuest(int index)
    {
        if (index == quest || index < 0 || index >= Chapter.Quests.Count)
            return;
        quest = index;
        BuildLines();
        Game1.playSound("smallSelect");
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton != null && upperRightCloseButton.containsPoint(x, y))
        {
            exitThisMenu();
            return;
        }
        for (int i = 0; i < chapterRows.Count; i++)
        {
            if (chapterRows[i].containsPoint(x, y))
            {
                SelectChapter(i, snap: false);
                return;
            }
        }
        for (int i = 0; i < questRows.Count; i++)
        {
            if (questRows[i].containsPoint(x, y))
            {
                SelectQuest(i);
                return;
            }
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        // With a controller the highlighted row is the selected one, so there's nothing extra to press.
        if (!Game1.options.gamepadControls)
            return;
        for (int i = 0; i < chapterRows.Count; i++)
            if (chapterRows[i].containsPoint(x, y))
                SelectChapter(i, snap: false);
        for (int i = 0; i < questRows.Count; i++)
            if (questRows[i].containsPoint(x, y))
                SelectQuest(i);
    }

    public override void receiveGamePadButton(Buttons b)
    {
        switch (b)
        {
            case Buttons.LeftShoulder:
                SelectChapter(chapter - 1, snap: true);
                break;
            case Buttons.RightShoulder:
                SelectChapter(chapter + 1, snap: true);
                break;
            case Buttons.LeftTrigger:
                Scroll(-3);
                break;
            case Buttons.RightTrigger:
                Scroll(3);
                break;
            default:
                base.receiveGamePadButton(b);
                break;
        }
    }

    public override void receiveScrollWheelAction(int direction) => Scroll(direction > 0 ? -2 : 2);

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.PageDown)
            Scroll(4);
        else if (key == Keys.PageUp)
            Scroll(-4);
        else
            base.receiveKeyPress(key);
    }

    private void Scroll(int delta)
    {
        int next = Math.Clamp(scroll + delta, 0, MaxScroll());
        if (next != scroll)
        {
            scroll = next;
            Game1.playSound("shiny4");
        }
    }

    /// <summary>Lowest first line that still fills the page.</summary>
    private int MaxScroll()
    {
        int h = 0;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            h += lines[i].Height;
            if (h > detailCol.Height)
                return i + 1;
        }
        return 0;
    }

    // ---- Page text ----

    private void BuildLines()
    {
        lines.Clear();
        scroll = 0;
        var q = Quest;
        int w = detailCol.Width;

        Paragraph(T($"guide.ch.{chapter}.intro"), Game1.smallFont, Game1.textShadowColor, w);
        Space(12);
        if (q == null)
            return;

        Paragraph(T($"guide.q.{q.Id}.title"), Game1.dialogueFont, Game1.textColor, w);
        bool isDone = done.Contains(q.Id);
        string status = isDone ? T("guide.done") : T("guide.todo");
        if (!isDone && GuideProgress.Counter(q.Goal, facts) is { } c)
            status += "  " + T("guide.counter", new { have = c.Have, need = c.Need });
        lines.Add(new Line(status, Game1.smallFont, isDone ? Color.DarkGreen : Game1.textColor, 44, Check: isDone));
        Space(8);

        // What Maru is waiting for before her next letter.
        if (q.Goal.Kind == GoalKind.ReadLetter && q.Goal.Tier is { } tier && !isDone)
        {
            if (gatesOn)
            {
                Paragraph(T("guide.req.header"), Game1.smallFont, Game1.textColor, w);
                // This tier's conditions, plus any earlier ones still missing (each tier also needs the ones before).
                var reqs = Enum.GetValues<TierId>().Where(t => t <= tier)
                    .SelectMany(t => GateRequirements.For(t, facts.Progress).Select(r => (Tier: t, Req: r)))
                    .Where(x => x.Tier == tier || !x.Req.Met)
                    .Select(x => x.Req);
                foreach (var r in reqs)
                    CheckLine(T($"guide.req.{r.Key}", new { have = Math.Min(r.Have, r.Need), need = r.Need }), r.Met, w);
                Paragraph(T("guide.req.morning"), Game1.smallFont, Game1.textShadowColor, w);
            }
            else
            {
                Paragraph(T("guide.req.gatesOff"), Game1.smallFont, Game1.textShadowColor, w);
            }
            Space(12);
        }

        foreach (string para in T($"guide.q.{q.Id}.text").Split('\n'))
        {
            if (para.Length == 0)
                Space(10);
            else
                Paragraph(para, Game1.smallFont, Game1.textColor, w);
        }

        if (q.Items.Count > 0)
        {
            Space(16);
            Paragraph(T("guide.items"), Game1.smallFont, Game1.textColor, w);
            foreach (string key in q.Items.Distinct())
                ItemLines(key, w);
        }
    }

    private void Space(int h) => lines.Add(new Line("", Game1.smallFont, Color.Transparent, h));

    private void Paragraph(string text, SpriteFont font, Color color, int w, int indent = 0)
    {
        int lineH = (int)font.MeasureString("Ay").Y;
        foreach (string row in Game1.parseText(text, font, w - indent).Split('\n'))
            lines.Add(new Line(row, font, color, lineH, indent));
    }

    private void CheckLine(string text, bool met, int w)
    {
        var rows = Game1.parseText(text, Game1.smallFont, w - 48).Split('\n');
        int lineH = (int)Game1.smallFont.MeasureString("Ay").Y;
        for (int i = 0; i < rows.Length; i++)
            lines.Add(new Line(rows[i], Game1.smallFont, met ? Color.DarkGreen : Game1.textColor, Math.Max(lineH, i == 0 ? 40 : 0), 48, Check: i == 0 ? met : null));
    }

    /// <summary>The part's icon and name, then its recipe as the game has it now (or the Processor Press inputs).</summary>
    private void ItemLines(string key, int w)
    {
        Item item;
        try
        {
            item = ItemRegistry.Create(ContentInjector.QualifiedIdOf(key));
        }
        catch (Exception)
        {
            return;
        }
        lines.Add(new Line(item.DisplayName, Game1.smallFont, Game1.textColor, 60, 60, Icon: item));

        string id = Ids.Prefix + key;
        if (i18n.Get($"guide.press.{key}").HasValue())
        {
            Paragraph(T($"guide.press.{key}"), Game1.smallFont, Game1.textShadowColor, w, 60);
            return;
        }
        if (!CraftingRecipe.craftingRecipes.ContainsKey(id))
        {
            Paragraph(T("guide.recipe.none"), Game1.smallFont, Game1.textShadowColor, w, 60);
            return;
        }

        var recipe = new CraftingRecipe(id, isCookingRecipe: false);
        string parts = string.Join(", ", recipe.recipeList.Select(p => $"{p.Value} {ItemRegistry.GetDataOrErrorItem(p.Key).DisplayName}"));
        if (recipe.numberProducedPerCraft > 1)
            parts += " " + T("guide.recipe.makes", new { n = recipe.numberProducedPerCraft });
        Paragraph(parts, Game1.smallFont, Game1.textColor, w, 60);
        bool known = Game1.player.craftingRecipes.ContainsKey(id);
        Paragraph(known ? T("guide.recipe.known") : T("guide.recipe.unknown"), Game1.smallFont, known ? Color.DarkGreen : Game1.textShadowColor, w, 60);
        Space(6);
    }

    // ---- Drawing ----

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds)
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);
        drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        int left = xPositionOnScreen + borderWidth;
        int top = yPositionOnScreen + borderWidth;

        // Header: title, total progress, next goal.
        Utility.drawTextWithShadow(b, T("guide.title"), Game1.dialogueFont, new Vector2(left, top - 8), Game1.textColor);
        int total = GuideBook.AllQuests.Count();
        string progress = T("guide.progress", new { done = done.Count, total });
        var titleSize = Game1.dialogueFont.MeasureString(T("guide.title"));
        b.DrawString(Game1.smallFont, progress, new Vector2(left + titleSize.X + 24, top + 8), Game1.textColor);
        var next = GuideProgress.Next(done);
        string nextText = next == null ? T("guide.allDone") : T("guide.next", new { goal = T($"guide.q.{next.Id}.title") });
        b.DrawString(Game1.smallFont, nextText, new Vector2(left, top + 44), Game1.textShadowColor);

        DrawChapters(b);
        DrawQuests(b);
        DrawDetails(b);

        // Footer hint.
        string hint = Game1.options.gamepadControls ? T("guide.hint.pad") : T("guide.hint.mouse");
        b.DrawString(Game1.smallFont, hint, new Vector2(left, yPositionOnScreen + height - borderWidth - FooterHeight + 4), Game1.textShadowColor);

        base.draw(b);
        drawMouse(b);
    }

    private void DrawChapters(SpriteBatch b)
    {
        for (int i = 0; i < chapterRows.Count; i++)
        {
            var c = GuideBook.Chapters[i];
            var r = chapterRows[i].bounds;
            bool open = c.Tier == null || facts.LettersRead.Contains(c.Tier.Value);
            bool finished = GuideProgress.ChapterDone(c, done);
            drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), r.X, r.Y, r.Width, r.Height, i == chapter ? Color.White : Color.White * 0.75f, 4f, false);
            if (i == chapter)
                b.Draw(Game1.staminaRect, new Rectangle(r.X + 8, r.Y + 8, r.Width - 16, r.Height - 16), Color.Wheat * 0.6f);

            string count = T("guide.chapterProgress", new { done = c.Quests.Count(q => done.Contains(q.Id)), total = c.Quests.Count });
            float countW = Game1.smallFont.MeasureString(count).X;
            string title = Game1.parseText(T($"guide.ch.{i}.title"), Game1.smallFont, r.Width - (int)countW - 40);
            var titleRows = title.Split('\n').Take(2).ToArray();
            int lineH = (int)Game1.smallFont.MeasureString("Ay").Y;
            int y = r.Center.Y - titleRows.Length * lineH / 2;
            Color color = open ? Game1.textColor : Game1.textColor * 0.5f;
            foreach (string row in titleRows)
            {
                b.DrawString(Game1.smallFont, row, new Vector2(r.X + 16, y), color);
                y += lineH;
            }
            if (finished)
                DrawCheck(b, r.Right - 48, r.Center.Y - 18, true);
            else
                b.DrawString(Game1.smallFont, count, new Vector2(r.Right - 16 - countW, r.Center.Y - lineH / 2), color);
        }
    }

    private void DrawQuests(SpriteBatch b)
    {
        int lineH = (int)Game1.smallFont.MeasureString("Ay").Y;
        for (int i = 0; i < questRows.Count; i++)
        {
            var q = Chapter.Quests[i];
            var r = questRows[i].bounds;
            if (i == quest)
                b.Draw(Game1.staminaRect, r, Color.Wheat * 0.6f);
            bool isDone = done.Contains(q.Id);
            DrawCheck(b, r.X + 4, r.Center.Y - 18, isDone);
            var rows = Game1.parseText(T($"guide.q.{q.Id}.title"), Game1.smallFont, r.Width - 56).Split('\n').Take(2).ToArray();
            int y = r.Center.Y - rows.Length * lineH / 2;
            foreach (string row in rows)
            {
                b.DrawString(Game1.smallFont, row, new Vector2(r.X + 52, y), isDone ? Game1.textColor * 0.6f : Game1.textColor);
                y += lineH;
            }
        }
    }

    private void DrawDetails(SpriteBatch b)
    {
        b.Draw(Game1.staminaRect, new Rectangle(detailCol.X - Gap / 2, detailCol.Y, 2, detailCol.Height), Game1.textShadowColor * 0.5f);
        int y = detailCol.Y;
        int bottom = detailCol.Bottom;
        int i = scroll;
        for (; i < lines.Count; i++)
        {
            var line = lines[i];
            if (y + line.Height > bottom)
                break;
            int x = detailCol.X + line.Indent;
            if (line.Icon != null)
            {
                line.Icon.drawInMenu(b, new Vector2(detailCol.X - 4, y - 4), 0.85f, 1f, 0.9f, StackDrawType.Hide);
                b.DrawString(Game1.smallFont, line.Text, new Vector2(x, y + 16), line.Color);
            }
            else if (line.Check is { } check)
            {
                DrawCheck(b, detailCol.X + (line.Indent > 0 ? 4 : 0), y, check);
                b.DrawString(Game1.smallFont, line.Text, new Vector2(line.Indent > 0 ? x : x + 48, y + 4), line.Color);
            }
            else if (line.Text.Length > 0)
            {
                b.DrawString(line.Font, line.Text, new Vector2(x, y), line.Color);
            }
            y += line.Height;
        }

        // Arrows when there's more above or below.
        if (scroll > 0)
            b.Draw(Game1.mouseCursors, new Vector2(detailCol.Right - 44, detailCol.Y - 8), new Rectangle(421, 459, 11, 12), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
        if (i < lines.Count)
            b.Draw(Game1.mouseCursors, new Vector2(detailCol.Right - 44, bottom - 28), new Rectangle(421, 472, 11, 12), Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1f);
    }

    private static void DrawCheck(SpriteBatch b, int x, int y, bool on) =>
        b.Draw(Game1.mouseCursors, new Vector2(x, y), on ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 1f);
}
