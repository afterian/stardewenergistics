using System;
using System.Collections.Generic;
using System.Linq;
using StardewEnergistics.Content;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace StardewEnergistics.Testing;

/// <summary>
/// Test mode (config <see cref="ModConfig.TestMode"/>, on until release): a controller-friendly cheat menu
/// opened with <see cref="ModConfig.TestMenuKey"/> (F7, or click both sticks). It mirrors se_unlock and se_give:
/// a free catalog of every mod item, unlock all recipes, a starter kit and crafting materials.
/// Works for each player on their own (host, farmhands and split-screen).
/// </summary>
internal sealed class TestMenu
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;

    /// <summary>Work to do once the question box has closed (per split-screen player).</summary>
    private readonly PerScreen<Action?> pending = new();

    public TestMenu(IModHelper helper, IMonitor monitor, ModConfig config)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;
    }

    public void Register()
    {
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.SaveLoaded += (_, _) =>
        {
            if (config.TestMode)
                Game1.addHUDMessage(new HUDMessage(T("test.hint"), HUDMessage.newQuest_type));
        };
    }

    private string T(string key) => helper.Translation.Get(key);

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!config.TestMode || !Context.IsPlayerFree || !config.TestMenuKey.JustPressed())
            return;
        helper.Input.SuppressActiveKeybinds(config.TestMenuKey);
        Open();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (pending.Value is not { } action || Game1.activeClickableMenu != null || Game1.dialogueUp)
            return;
        pending.Value = null;
        action();
    }

    public void Open()
    {
        var choices = new[]
        {
            new Response("catalog", T("test.catalog")),
            new Response("kit", T("test.kit")),
            new Response("materials", T("test.materials")),
            new Response("unlock", T("test.unlock")),
            new Response("close", T("test.close")),
        };
        Game1.currentLocation.createQuestionDialogue(T("test.title"), choices, (_, answer) =>
        {
            pending.Value = answer switch
            {
                "catalog" => OpenCatalog,
                "kit" => () => { TestTools.GiveSet(TestTools.StarterKit); Hud(T("test.kit.done")); },
                "materials" => () => { TestTools.GiveSet(TestTools.Materials); Hud(T("test.materials.done")); },
                "unlock" => () =>
                {
                    int added = TestTools.Unlock().Count;
                    Hud(helper.Translation.Get("test.unlock.done", new { count = added }));
                },
                _ => null,
            };
        });
    }

    private static void Hud(string text) => Game1.addHUDMessage(new HUDMessage(text) { noIcon = true });

    /// <summary>A free shop with every mod item, then the vanilla materials. Buy as many as you like.</summary>
    private void OpenCatalog()
    {
        var stock = new Dictionary<ISalable, ItemStockInformation>();
        var ids = ContentInjector.AllQualifiedItemIds.Concat(TestTools.Materials.Select(m => m.Id)).Distinct();
        foreach (string id in ids.Where(id => ItemRegistry.GetData(id) != null))
            stock[ItemRegistry.Create(id)] = new ItemStockInformation(0, ShopMenu.infiniteStock);
        Game1.activeClickableMenu = new ShopMenu(Ids.ModId + "_TestCatalog", stock);
        monitor.Log($"Opened the test catalog ({stock.Count} items).", LogLevel.Trace);
    }
}
