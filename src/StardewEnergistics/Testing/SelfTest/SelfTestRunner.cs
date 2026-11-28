using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>
/// In-game integration self-test. <c>se_selftest</c> in the SMAPI console runs every scenario (or the ones
/// named) in a scratch location, through the real game and mod code paths, and writes a pass/fail report.
/// Launched with the environment variable <c>SE_SELFTEST=1</c> (see deploy/pc/selftest.ps1) it loads a save
/// by itself, runs everything, writes the report and quits the game, never saving.
/// </summary>
internal sealed class SelfTestRunner
{
    private const string Marker = "SE-SELFTEST";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly SelfTestServices services;

    /// <summary>Launched for the self-test: load a save, run, quit (SE_SELFTEST=1).</summary>
    private readonly bool auto = Environment.GetEnvironmentVariable("SE_SELFTEST") == "1";

    private readonly Screenshots shots;
    private Run? run;
    private bool loadRequested;
    private int readyTicks;
    private int titleTicks;

    public SelfTestRunner(IModHelper helper, IMonitor monitor, ModConfig config, SelfTestServices services)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;
        this.services = services;
        shots = new Screenshots(helper, monitor);
    }

    public void Register()
    {
        helper.ConsoleCommands.Add("se_selftest",
            "Run the in-game self-test in a scratch location and write a pass/fail report.\n"
            + "Usage: se_selftest [scenario ...] | se_selftest list\n"
            + "Never saves; your backpack and position are put back afterwards. Scenarios that would touch your other networks only run in a session launched by deploy/pc/selftest.ps1.",
            OnCommand);
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.Saving += (_, _) =>
        {
            if (run != null)
                monitor.Log("The game is saving while the self-test runs; the scratch location is removed before the next save.", LogLevel.Warn);
        };
        if (auto)
            monitor.Log($"{Marker}: launched for the self-test; a save will load by itself.", LogLevel.Info);
    }

    private void OnCommand(string command, string[] args)
    {
        if (args.FirstOrDefault() == "list")
        {
            foreach (var s in Scenarios.All)
                monitor.Log($"  {s.Id,-14} {s.Title}{(s.ThrowawayOnly ? " (launched sessions only)" : "")}", LogLevel.Info);
            return;
        }
        if (!config.TestMode && !auto)
        {
            monitor.Log("The self-test needs test mode (TestMode in config.json).", LogLevel.Warn);
            return;
        }
        if (!Context.IsWorldReady)
        {
            monitor.Log("Load a save first.", LogLevel.Warn);
            return;
        }
        if (run != null)
        {
            monitor.Log("The self-test is already running.", LogLevel.Warn);
            return;
        }
        var unknown = args.Where(a => Scenarios.All.All(s => s.Id != a)).ToList();
        if (unknown.Count > 0)
        {
            monitor.Log($"Unknown scenario(s): {string.Join(", ", unknown)}. Try se_selftest list.", LogLevel.Warn);
            return;
        }
        Start(args);
    }

    private void Start(IReadOnlyCollection<string> only)
    {
        var chosen = Scenarios.All.Where(s => only.Count == 0 ? !s.OnlyWhenNamed : only.Contains(s.Id)).ToList();
        run = new Run(helper, monitor, services, chosen, throwaway: auto, shots);
        monitor.Log($"{Marker}: starting {chosen.Count} scenarios.", LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (run != null)
        {
            if (run.Tick())
            {
                var report = run.Finish();
                run = null;
                WriteReport(report);
                if (auto)
                    Quit(report.Failed == 0 && report.Crashed == 0 ? 0 : 1);
            }
            return;
        }

        if (!auto || loadRequested && Context.IsWorldReady && readyTicks < 0)
            return;
        AutoStep();
    }

    /// <summary>Launched mode: load a save from the title screen, then start once the player is free.</summary>
    private void AutoStep()
    {
        if (!Context.IsWorldReady)
        {
            if (loadRequested || Game1.activeClickableMenu is not TitleMenu || TitleMenu.subMenu != null)
                return;
            if (++titleTicks < 60)
                return;
            string? save = PickSave();
            if (save == null)
            {
                monitor.Log($"{Marker}: no save to load. Start a farm once, or set SE_SELFTEST_SAVE to a save folder name.", LogLevel.Error);
                WriteReport(Report.NoSave());
                Quit(2);
                return;
            }
            monitor.Log($"{Marker}: loading save {save} (it will not be saved).", LogLevel.Info);
            loadRequested = true;
            Game1.multiplayerMode = 0;
            SaveGame.Load(save);
            Game1.exitActiveMenu();
            return;
        }

        // Let the day start settle, close anything that opened, then go.
        if (Game1.CurrentEvent != null && Game1.eventUp)
        {
            Game1.CurrentEvent.skipEvent();
            return;
        }
        if (Game1.activeClickableMenu != null && readyTicks > 120)
        {
            Game1.exitActiveMenu();
            return;
        }
        if (++readyTicks < 180 || !Context.IsPlayerFree)
            return;
        readyTicks = -1;
        var only = (Environment.GetEnvironmentVariable("SE_SELFTEST_ONLY") ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        Start(only);
    }

    /// <summary>The save named by SE_SELFTEST_SAVE, else the most recently played one.</summary>
    private static string? PickSave()
    {
        string? named = Environment.GetEnvironmentVariable("SE_SELFTEST_SAVE");
        if (!Directory.Exists(Constants.SavesPath))
            return null;
        var saves = Directory.GetDirectories(Constants.SavesPath)
            .Where(d => File.Exists(Path.Combine(d, Path.GetFileName(d))))
            .OrderByDescending(d => File.GetLastWriteTimeUtc(Path.Combine(d, Path.GetFileName(d))))
            .Select(Path.GetFileName)
            .ToList();
        if (!string.IsNullOrWhiteSpace(named))
            return saves.FirstOrDefault(s => string.Equals(s, named, StringComparison.OrdinalIgnoreCase));
        return saves.FirstOrDefault();
    }

    private void Quit(int code)
    {
        monitor.Log($"{Marker}: closing the game (exit code {code}).", LogLevel.Info);
        Environment.Exit(code);
    }

    private void WriteReport(Report report)
    {
        string text = report.ToText();
        foreach (string line in text.Split('\n'))
            monitor.Log(line, line.StartsWith("[FAIL]") || line.StartsWith("[CRASH]") ? LogLevel.Error : LogLevel.Info);

        string path = Environment.GetEnvironmentVariable("SE_SELFTEST_REPORT") is { Length: > 0 } p ? p : Path.Combine(helper.DirectoryPath, "selftest-report.txt");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text, new UTF8Encoding(false));
            File.WriteAllText(Path.ChangeExtension(path, ".json"), report.ToJson(), new UTF8Encoding(false));
            monitor.Log($"Report written to {path}", LogLevel.Info);
        }
        catch (Exception e)
        {
            monitor.Log($"Couldn't write the report to {path}: {e.Message}", LogLevel.Error);
        }
    }

    // ---------------------------------------------------------------

    /// <summary>One run: sets up the scratch location, steps through scenarios tick by tick, then puts everything back.</summary>
    private sealed class Run
    {
        private readonly IModHelper helper;
        private readonly IMonitor monitor;
        private readonly SelfTestServices services;
        private readonly List<Scenario> scenarios;
        private readonly bool throwaway;
        private readonly List<CheckResult> results = new();
        private readonly List<(string Id, string Status, string Detail)> outcomes = new();
        private readonly DateTime started = DateTime.Now;

        // What to put back.
        private readonly GameLocation homeLocation;
        private readonly Vector2 homePosition;
        private readonly int homeFacing;
        private readonly List<Item?> homeItems;
        private readonly int homeToolIndex;
        private readonly float homeStamina;

        private GameLocation? scratch;
        private SelfTestContext? ctx;
        private int index = -1;
        private IEnumerator<object?>? steps;
        private int waitTicks;
        private WaitUntil? waitUntil;
        private int waitUntilTicks;
        private int setupTicks;
        private bool warped;

        private readonly Screenshots shots;

        public Run(IModHelper helper, IMonitor monitor, SelfTestServices services, List<Scenario> scenarios, bool throwaway, Screenshots shots)
        {
            this.shots = shots;
            this.helper = helper;
            this.monitor = monitor;
            this.services = services;
            this.scenarios = scenarios;
            this.throwaway = throwaway;
            homeLocation = Game1.currentLocation;
            homePosition = Game1.player.Position;
            homeFacing = Game1.player.FacingDirection;
            homeItems = Game1.player.Items.ToList();
            homeToolIndex = Game1.player.CurrentToolIndex;
            homeStamina = Game1.player.Stamina;
        }

        /// <summary>Advances one tick. Returns true when the run is over.</summary>
        public bool Tick()
        {
            if (ctx == null)
                return Setup();

            if (shots.Busy)
                return false; // let the screenshot be taken before anything changes
            if (waitTicks > 0)
            {
                waitTicks--;
                return false;
            }
            if (waitUntil != null)
            {
                if (!waitUntil.Condition())
                {
                    if (++waitUntilTicks < waitUntil.TimeoutTicks)
                        return false;
                    ctx.Check($"timed out waiting: {waitUntil.What}", false);
                    EndScenario("fail", "timed out");
                    return NextScenario();
                }
                waitUntil = null;
            }

            if (steps == null)
                return NextScenario();

            try
            {
                if (!steps.MoveNext())
                {
                    EndScenario(null, "");
                    return NextScenario();
                }
                switch (steps.Current)
                {
                    case int n:
                        waitTicks = Math.Max(0, n - 1);
                        break;
                    case WaitUntil w:
                        waitUntil = w;
                        waitUntilTicks = 0;
                        break;
                }
            }
            catch (ScenarioAbort e)
            {
                EndScenario("fail", e.Message);
                return NextScenario();
            }
            catch (Exception e)
            {
                ctx.Check("no exception", false, e.ToString());
                EndScenario("crash", e.GetBaseException().Message);
                return NextScenario();
            }
            return false;
        }

        /// <summary>Makes the scratch location and warps there. Returns true only if setup failed for good.</summary>
        private bool Setup()
        {
            if (scratch == null)
            {
                Game1.player.Items.OverwriteWith(new List<Item?>(new Item?[Game1.player.Items.Count]));
                scratch = new GameLocation("Maps\\Shed2", SelfTestContext.LocationName);
                Game1.locations.Add(scratch);
                if (FindArea(scratch) is not { } origin)
                {
                    outcomes.Add(("setup", "crash", "no free area in the scratch location"));
                    return true;
                }
                ctx = null;
                pendingOrigin = origin;
                Game1.warpFarmer(SelfTestContext.LocationName, (int)origin.X, (int)origin.Y + 2, false);
                return false;
            }

            if (Game1.currentLocation != scratch || Game1.isWarping || Game1.globalFade || !Context.IsPlayerFree)
            {
                if (++setupTicks > 600)
                {
                    outcomes.Add(("setup", "crash", "couldn't warp to the scratch location"));
                    return true;
                }
                return false;
            }
            warped = true;
            ctx = new SelfTestContext(helper, monitor, services, scratch, pendingOrigin, throwaway, results) { Shots = shots };
            return false;
        }

        private Vector2 pendingOrigin;

        /// <summary>Top-left of a free, walkable block of tiles big enough for every scenario.</summary>
        private static Vector2? FindArea(GameLocation loc)
        {
            var map = loc.Map.Layers[0];
            for (int y = 1; y + SelfTestContext.AreaHeight < map.LayerHeight; y++)
            {
                for (int x = 1; x + SelfTestContext.AreaWidth < map.LayerWidth; x++)
                {
                    bool ok = true;
                    for (int dy = 0; dy < SelfTestContext.AreaHeight && ok; dy++)
                        for (int dx = 0; dx < SelfTestContext.AreaWidth && ok; dx++)
                        {
                            var t = new Vector2(x + dx, y + dy);
                            ok = loc.isTilePassable(t) && !loc.isWaterTile(x + dx, y + dy) && loc.getTileIndexAt(x + dx, y + dy, "Buildings") < 0;
                        }
                    if (ok)
                        return new Vector2(x, y);
                }
            }
            return null;
        }

        private bool NextScenario()
        {
            index++;
            steps = null;
            waitTicks = 0;
            waitUntil = null;
            if (index >= scenarios.Count)
                return true;

            var s = scenarios[index];
            ctx!.Scenario = s.Id;
            if (s.ThrowawayOnly && !throwaway)
            {
                outcomes.Add((s.Id, "skip", "runs only in a session launched for the self-test"));
                return NextScenario();
            }
            monitor.Log($"[self-test] {s.Id}: {s.Title}", LogLevel.Debug);
            try
            {
                ctx.Reset();
                steps = Prelude(ctx, s).GetEnumerator();
            }
            catch (Exception e)
            {
                ctx.Check("set up the scenario", false, e.ToString());
                EndScenario("crash", e.GetBaseException().Message);
                return NextScenario();
            }
            return false;
        }

        /// <summary>
        /// SMAPI reports the objects Reset removed on the next tick, and the mod drops their contents then:
        /// wait for that and sweep the ground again, so a scenario starts with nothing lying around.
        /// </summary>
        private static IEnumerable<object?> Prelude(SelfTestContext c, Scenario s)
        {
            yield return Wait.Ticks(2);
            c.Loc.debris.Clear();
            Game1.hudMessages.Clear();
            foreach (object? step in s.Run(c))
                yield return step;
        }

        private void EndScenario(string? status, string detail)
        {
            var s = scenarios[index];
            var mine = results.Where(r => r.Scenario == s.Id).ToList();
            status ??= mine.All(r => r.Passed) ? "pass" : "fail";
            if (status == "pass" && mine.Count == 0)
                status = "fail";
            outcomes.Add((s.Id, status, detail));
            steps?.Dispose();
            steps = null;
        }

        /// <summary>Puts the player, the backpack and the world back as they were, and builds the report.</summary>
        public Report Finish()
        {
            try
            {
                if (Game1.activeClickableMenu != null)
                    Game1.exitActiveMenu();
                ctx?.Reset();
                if (ctx != null)
                {
                    // Empty every disk the tests made, so nothing is left in save data.
                    foreach (string id in ctx.DiskIds)
                    {
                        foreach (var tier in Enum.GetValues<Core.Storage.DiskTier>())
                        {
                            var d = services.Disks.Get(id, tier);
                            foreach (var (key, count) in d.Stacks.ToList())
                                d.Take(key, count);
                        }
                    }
                }
                if (scratch != null)
                {
                    if (warped && Game1.currentLocation == scratch)
                        Game1.warpFarmer(homeLocation.NameOrUniqueName, (int)(homePosition.X / 64), (int)(homePosition.Y / 64), homeFacing);
                    // Removed once the warp has left it (see RemoveScratch).
                    pendingRemoval = scratch;
                    helper.Events.GameLoop.UpdateTicked += RemoveScratch;
                }
                Game1.player.Items.OverwriteWith(homeItems);
                Game1.player.CurrentToolIndex = homeToolIndex;
                Game1.player.Stamina = homeStamina;
                services.Networks.Clear();
                shots.WriteIndex();
            }
            catch (Exception e)
            {
                outcomes.Add(("cleanup", "crash", e.ToString()));
            }
            return new Report(started, DateTime.Now, results, outcomes, scenarios.Count, throwaway);
        }

        private GameLocation? pendingRemoval;

        private void RemoveScratch(object? sender, UpdateTickedEventArgs e)
        {
            if (pendingRemoval == null || Game1.currentLocation == pendingRemoval || Game1.isWarping)
                return;
            Game1.locations.Remove(pendingRemoval);
            services.Wires.For(pendingRemoval).Clear();
            services.Networks.Clear();
            pendingRemoval = null;
            helper.Events.GameLoop.UpdateTicked -= RemoveScratch;
        }
    }

    /// <summary>The run's results as text (for people and the SMAPI log) and JSON (for scripts).</summary>
    private sealed class Report
    {
        public Report(DateTime started, DateTime ended, List<CheckResult> checks, List<(string Id, string Status, string Detail)> outcomes, int scenarios, bool throwaway)
        {
            Started = started;
            Ended = ended;
            Checks = checks;
            Outcomes = outcomes;
            Scenarios = scenarios;
            Throwaway = throwaway;
        }

        public DateTime Started { get; }
        public DateTime Ended { get; }
        public List<CheckResult> Checks { get; }
        public List<(string Id, string Status, string Detail)> Outcomes { get; }
        public int Scenarios { get; }
        public bool Throwaway { get; }
        public string? Error { get; init; }

        public int Failed => Outcomes.Count(o => o.Status == "fail");
        public int Crashed => Outcomes.Count(o => o.Status == "crash");
        public int Passed => Outcomes.Count(o => o.Status == "pass");
        public int Skipped => Outcomes.Count(o => o.Status == "skip");

        public static Report NoSave() => new(DateTime.Now, DateTime.Now, new(), new() { ("setup", "crash", "no save to load") }, 0, true) { Error = "no save to load" };

        public string Result => Failed == 0 && Crashed == 0 && Passed > 0 ? "PASS" : "FAIL";

        public string ToText()
        {
            var sb = new StringBuilder();
            string version = typeof(SelfTestRunner).Assembly.GetName().Version?.ToString() ?? "?";
            sb.Append($"Stardew Energistics self-test, {Started:yyyy-MM-dd HH:mm:ss}, {(Ended - Started).TotalSeconds:0.0}s, mod {version}, Stardew {Game1.version}, {(Throwaway ? "launched session" : "console run")}\n");
            foreach (var (id, status, detail) in Outcomes)
            {
                string tag = status.ToUpperInvariant();
                var checks = Checks.Where(c => c.Scenario == id).ToList();
                sb.Append($"[{tag}] {id}: {checks.Count(c => c.Passed)}/{checks.Count} checks{(detail.Length > 0 ? " - " + detail : "")}\n");
                foreach (var c in checks.Where(c => !c.Passed))
                    sb.Append($"    x {c.Name}{(c.Detail.Length > 0 ? ": " + c.Detail.Split('\n')[0] : "")}\n");
            }
            sb.Append($"{Marker} RESULT: {Result} ({Passed} passed, {Failed} failed, {Crashed} crashed, {Skipped} skipped; {Checks.Count(c => c.Passed)}/{Checks.Count} checks)\n");
            return sb.ToString();
        }

        public string ToJson() => System.Text.Json.JsonSerializer.Serialize(new
        {
            result = Result,
            started = Started,
            seconds = (Ended - Started).TotalSeconds,
            launched = Throwaway,
            error = Error,
            scenarios = Outcomes.Select(o => new
            {
                id = o.Id,
                status = o.Status,
                detail = o.Detail,
                checks = Checks.Where(c => c.Scenario == o.Id).Select(c => new { name = c.Name, passed = c.Passed, detail = c.Detail }),
            }),
        }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
