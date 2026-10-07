using Avalonia.Input;
using Robotron.Core.Input;
using Robotron.Infrastructure.Input;
using Robotron.Infrastructure.Persistence;

namespace Robotron.Avalonia.Input;

/// Translates raw keyboard/gamepad state into logical inputs. Held keys are a set, so OS key-repeat
/// KeyDown events cannot double an input; buttons are edge-detected once per poll.
public sealed class InputMapper
{
    readonly HashSet<Key> _held = [];
    readonly HashSet<Key> _pressedSincePoll = [];
    Dictionary<Key, List<InputAction>> _keyToActions = [];
    GamepadState _pad, _prevPad;

    public InputMapper(Dictionary<InputAction, List<string>> bindings) => SetBindings(bindings);

    public float Deadzone { get; set; } = 0.2f;
    public bool AnalogMove { get; set; }
    public bool AnalogFire { get; set; }
    public GamepadState Gamepad => _pad;

    public void SetBindings(Dictionary<InputAction, List<string>> bindings)
    {
        var map = new Dictionary<Key, List<InputAction>>();
        foreach (var (action, keys) in bindings)
            foreach (var name in keys)
                if (Enum.TryParse<Key>(name, ignoreCase: true, out var k))
                    (map.TryGetValue(k, out var l) ? l : map[k] = []).Add(action);
        _keyToActions = map;
    }

    public void KeyDown(Key k) { if (_held.Add(k)) _pressedSincePoll.Add(k); }
    public void KeyUp(Key k) => _held.Remove(k);
    /// Window lost focus: drop held keys so nothing sticks.
    public void ReleaseAll() => _held.Clear();

    public void UpdateGamepad(GamepadState s) { _prevPad = _pad; _pad = s; }

    bool Held(InputAction a) => _held.Any(k => _keyToActions.TryGetValue(k, out var l) && l.Contains(a));

    /// True once per physical press (keyboard or gamepad). Consumes the press.
    public bool Pressed(InputAction a)
    {
        foreach (var k in _pressedSincePoll)
            if (_keyToActions.TryGetValue(k, out var l) && l.Contains(a)) { _pressedSincePoll.Remove(k); return true; }
        return a switch
        {
            InputAction.Start => Edge(_pad.Start, _prevPad.Start) || Edge(_pad.South, _prevPad.South),
            InputAction.Pause => Edge(_pad.Start, _prevPad.Start) || Edge(_pad.Back, _prevPad.Back),
            InputAction.Help => Edge(_pad.North, _prevPad.North),
            _ => false,
        };
    }

    /// Menu navigation: returns -1/0/+1 per axis on a fresh press (keys, d-pad, or either stick).
    public (int dx, int dy) MenuNudge()
    {
        int dx = 0, dy = 0;
        if (PressedRaw(InputAction.MoveUp) || PressedRaw(InputAction.FireUp) || Edge(_pad.DpadUp, _prevPad.DpadUp) || StickEdge(_pad.LeftY, _prevPad.LeftY) < 0) dy = -1;
        if (PressedRaw(InputAction.MoveDown) || PressedRaw(InputAction.FireDown) || Edge(_pad.DpadDown, _prevPad.DpadDown) || StickEdge(_pad.LeftY, _prevPad.LeftY) > 0) dy = 1;
        if (PressedRaw(InputAction.MoveLeft) || PressedRaw(InputAction.FireLeft) || Edge(_pad.DpadLeft, _prevPad.DpadLeft) || StickEdge(_pad.LeftX, _prevPad.LeftX) < 0) dx = -1;
        if (PressedRaw(InputAction.MoveRight) || PressedRaw(InputAction.FireRight) || Edge(_pad.DpadRight, _prevPad.DpadRight) || StickEdge(_pad.LeftX, _prevPad.LeftX) > 0) dx = 1;
        return (dx, dy);
    }

    public bool BackPressed() => Pressed(InputAction.Pause) || Edge(_pad.East, _prevPad.East);

    /// Clears edge state at the end of a UI frame so a press is not seen twice.
    public void EndFrame() => _pressedSincePoll.Clear();

    bool PressedRaw(InputAction a) =>
        _pressedSincePoll.Any(k => _keyToActions.TryGetValue(k, out var l) && l.Contains(a));

    static bool Edge(bool now, bool before) => now && !before;
    static int StickEdge(float now, float before) =>
        Math.Abs(now) > 0.6f && Math.Abs(before) <= 0.6f ? Math.Sign(now) : 0;

    /// Build the per-tick game input. Keyboard and d-pad are digital switches; sticks are analog,
    /// collapsed to 8-way unless the policy allows analog for that stick.
    public TickInput BuildTickInput()
    {
        var move = StickQuantizer.Digital(
            Held(InputAction.MoveLeft) || _pad.DpadLeft, Held(InputAction.MoveRight) || _pad.DpadRight,
            Held(InputAction.MoveUp) || _pad.DpadUp, Held(InputAction.MoveDown) || _pad.DpadDown);
        var fire = StickQuantizer.Digital(
            Held(InputAction.FireLeft), Held(InputAction.FireRight), Held(InputAction.FireUp), Held(InputAction.FireDown));

        if (move.IsNeutral && _pad.Connected)
            move = AnalogMove ? StickQuantizer.Analog(_pad.LeftX, _pad.LeftY, Deadzone) : StickQuantizer.EightWay(_pad.LeftX, _pad.LeftY, Deadzone);
        if (fire.IsNeutral && _pad.Connected)
            fire = AnalogFire ? StickQuantizer.Analog(_pad.RightX, _pad.RightY, Deadzone) : StickQuantizer.EightWay(_pad.RightX, _pad.RightY, Deadzone);
        return new TickInput(move, fire);
    }
}
