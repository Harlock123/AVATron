using Silk.NET.SDL;
using AVATron.Infrastructure.Platform;

namespace AVATron.Infrastructure.Input;

/// Snapshot of the first connected gamepad. Axes are -1..1 with Y pointing down (screen space).
public readonly record struct GamepadState(
    bool Connected, string Name,
    float LeftX, float LeftY, float RightX, float RightY,
    bool DpadUp, bool DpadDown, bool DpadLeft, bool DpadRight,
    bool Start, bool Back, bool South, bool East, bool North, bool West)
{
    public static readonly GamepadState Disconnected = new(false, "", 0, 0, 0, 0, false, false, false, false, false, false, false, false, false, false);
}

public interface IGamepadSource : IDisposable
{
    string Status { get; }
    GamepadState Poll();
}

public sealed class NoGamepadSource(string reason) : IGamepadSource
{
    public string Status { get; } = reason;
    public GamepadState Poll() => GamepadState.Disconnected;
    public void Dispose() { }
}

/// SDL2 GameController reader. Handles hot-plug: devices are opened on CONTROLLERDEVICEADDED and
/// released on CONTROLLERDEVICEREMOVED, with no restart required. Call Poll() from one thread only.
public sealed unsafe class SdlGamepadSource : IGamepadSource
{
    readonly Sdl _sdl;
    GameController* _pad;
    int _padInstance = -1;
    string _name = "";

    SdlGamepadSource(Sdl sdl) { _sdl = sdl; OpenFirstAvailable(); }

    public string Status => _pad is null ? "No gamepad connected" : $"Gamepad: {_name}";

    public static IGamepadSource TryCreate()
    {
        try
        {
            var sdl = SdlHost.Instance.TryInitSubsystem(Sdl.InitGamecontroller, out var err);
            return sdl is null ? new NoGamepadSource($"Gamepad support unavailable: {err}") : new SdlGamepadSource(sdl);
        }
        catch (Exception ex)
        {
            return new NoGamepadSource($"Gamepad support unavailable: {ex.Message}");
        }
    }

    public GamepadState Poll()
    {
        Event ev;
        // Only drain controller-device events; leave anything else (e.g. audio) to SDL.
        _sdl.PumpEvents();
        while (_sdl.PeepEvents(&ev, 1, Eventaction.Getevent, (uint)EventType.Controllerdeviceadded, (uint)EventType.Controllerdeviceremapped) > 0)
        {
            if (ev.Type == (uint)EventType.Controllerdeviceadded && _pad is null) OpenFirstAvailable();
            else if (ev.Type == (uint)EventType.Controllerdeviceremoved && ev.Cdevice.Which == _padInstance) { Close(); OpenFirstAvailable(); }
        }
        if (_pad is null || _sdl.GameControllerGetAttached(_pad) == SdlBool.False)
        {
            if (_pad is not null) Close();
            return GamepadState.Disconnected;
        }
        return new GamepadState(true, _name,
            Axis(GameControllerAxis.Leftx), Axis(GameControllerAxis.Lefty),
            Axis(GameControllerAxis.Rightx), Axis(GameControllerAxis.Righty),
            Btn(GameControllerButton.DpadUp), Btn(GameControllerButton.DpadDown),
            Btn(GameControllerButton.DpadLeft), Btn(GameControllerButton.DpadRight),
            Btn(GameControllerButton.Start), Btn(GameControllerButton.Back),
            Btn(GameControllerButton.A), Btn(GameControllerButton.B),
            Btn(GameControllerButton.Y), Btn(GameControllerButton.X));
    }

    float Axis(GameControllerAxis a) => Math.Clamp(_sdl.GameControllerGetAxis(_pad, a) / 32767f, -1f, 1f);
    bool Btn(GameControllerButton b) => _sdl.GameControllerGetButton(_pad, b) != 0;

    void OpenFirstAvailable()
    {
        int n = _sdl.NumJoysticks();
        for (int i = 0; i < n; i++)
        {
            if (_sdl.IsGameController(i) != SdlBool.True) continue;
            var pad = _sdl.GameControllerOpen(i);
            if (pad is null) continue;
            _pad = pad;
            _padInstance = _sdl.JoystickInstanceID(_sdl.GameControllerGetJoystick(pad));
            _name = _sdl.GameControllerNameS(pad) ?? "Gamepad";
            return;
        }
    }

    void Close()
    {
        if (_pad is not null) _sdl.GameControllerClose(_pad);
        _pad = null; _padInstance = -1; _name = "";
    }

    public void Dispose() => Close();
}
