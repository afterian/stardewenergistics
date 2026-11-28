using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewEnergistics.Testing.SelfTest;

/// <summary>A cursor the self-test points wherever it likes.</summary>
internal sealed class FakeCursor : ICursorPosition
{
    public FakeCursor(Vector2 tile, Vector2 grabTile)
    {
        Tile = tile;
        GrabTile = grabTile;
        AbsolutePixels = tile * Game1.tileSize + new Vector2(Game1.tileSize / 2f);
        ScreenPixels = AbsolutePixels - new Vector2(Game1.viewport.X, Game1.viewport.Y);
    }

    public Vector2 AbsolutePixels { get; }
    public Vector2 ScreenPixels { get; }
    public Vector2 Tile { get; }
    public Vector2 GrabTile { get; }
    public Vector2 GetScaledAbsolutePixels() => AbsolutePixels;
    public Vector2 GetScaledScreenPixels() => ScreenPixels;
    public bool Equals(ICursorPosition? other) => other != null && other.Tile == Tile && other.GrabTile == GrabTile;
}

/// <summary>
/// Sends a button press to the mod's own input handlers, in the order ModEntry registers them, exactly as
/// SMAPI would (a handler that suppresses the button stops the later ones from acting, through the real
/// input helper). The game's own handling of the press isn't simulated: a check that our handler
/// suppressed the button stands in for "the game didn't also act on it".
/// </summary>
internal sealed class SyntheticInput
{
    private readonly IModHelper helper;
    private readonly List<(object Target, MethodInfo Pressed, MethodInfo? Released)> handlers = new();
    private readonly ConstructorInfo pressedCtor;
    private readonly ConstructorInfo releasedCtor;

    public SyntheticInput(IModHelper helper, IEnumerable<object> targets)
    {
        this.helper = helper;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (object t in targets)
        {
            var pressed = t.GetType().GetMethod("OnButtonPressed", flags)
                ?? throw new InvalidOperationException($"{t.GetType().Name} has no OnButtonPressed handler.");
            handlers.Add((t, pressed, t.GetType().GetMethod("OnButtonReleased", flags)));
        }
        pressedCtor = typeof(ButtonPressedEventArgs).GetConstructors(flags)[0];
        releasedCtor = typeof(ButtonReleasedEventArgs).GetConstructors(flags)[0];
    }

    /// <summary>
    /// Presses and releases <paramref name="button"/>. <paramref name="mouse"/> true acts like a visible mouse
    /// cursor on <paramref name="cursorTile"/>; false like a controller with the cursor hidden.
    /// Returns true if one of our handlers suppressed the button (took the press).
    /// </summary>
    public bool Press(SButton button, Vector2 cursorTile, Vector2? grabTile = null, bool mouse = true)
    {
        ClearSuppression(button);
        var cursor = new FakeCursor(cursorTile, grabTile ?? cursorTile);
        SetPointer(mouse);
        foreach (var (target, pressed, _) in handlers)
        {
            SetPointer(mouse); // a handler may read these; the game resets them each frame
            Invoke(pressed, target, pressedCtor.Invoke(new object?[] { button, cursor, null }));
        }
        bool taken = helper.Input.IsSuppressed(button);
        foreach (var (target, _, released) in handlers)
        {
            if (released != null)
                Invoke(released, target, releasedCtor.Invoke(new object?[] { button, cursor, null }));
        }
        ClearSuppression(button);
        return taken;
    }

    private static void Invoke(MethodInfo method, object target, object args)
    {
        try
        {
            method.Invoke(target, new[] { null, args });
        }
        catch (TargetInvocationException e) when (e.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException).Throw();
        }
    }

    private static void SetPointer(bool mouse)
    {
        Game1.lastCursorMotionWasMouse = mouse;
        Game1.mouseCursorTransparency = mouse ? 1f : 0f;
        Game1.wasMouseVisibleThisFrame = mouse;
    }

    /// <summary>Forget a suppression left on a button that was never really pressed.</summary>
    private void ClearSuppression(SButton button)
    {
        try
        {
            var getState = helper.Input.GetType().GetField("CurrentInputState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(helper.Input) as Delegate;
            object? state = getState?.DynamicInvoke();
            if (state?.GetType().GetField("CustomReleasedKeys", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(state) is HashSet<SButton> released)
                released.Remove(button);
        }
        catch
        {
            // Best effort; a stale suppression shows up as a failed check.
        }
    }
}
