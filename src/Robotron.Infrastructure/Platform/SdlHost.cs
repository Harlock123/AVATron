using Silk.NET.SDL;

namespace Robotron.Infrastructure.Platform;

/// Owns SDL2 initialisation. SDL is optional: if the native library is missing or a subsystem
/// fails to start, callers get null plus a reason and the game continues without that feature.
public sealed class SdlHost
{
    public static SdlHost Instance { get; } = new();
    readonly object _gate = new();
    Sdl? _sdl;
    bool _loadFailed;
    string _loadError = "";

    public Sdl? TryInitSubsystem(uint flags, out string error)
    {
        lock (_gate)
        {
            error = "";
            if (_loadFailed) { error = _loadError; return null; }
            try
            {
                _sdl ??= Sdl.GetApi();
                // Let gamepads be read while the Avalonia window (not an SDL window) has focus.
                _sdl.SetHint(Sdl.HintJoystickAllowBackgroundEvents, "1");
                if (_sdl.InitSubSystem(flags) != 0) { error = _sdl.GetErrorS(); return null; }
                return _sdl;
            }
            catch (Exception ex)
            {
                _loadFailed = true;
                _loadError = error = $"SDL2 could not be loaded ({ex.GetType().Name}: {ex.Message})";
                return null;
            }
        }
    }
}
