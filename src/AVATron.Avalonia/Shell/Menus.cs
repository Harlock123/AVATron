using Avalonia.Input;
using AVATron.Avalonia.Rendering;
using AVATron.Core.Scoring;
using AVATron.Core.Simulation;
using AVATron.Core.Sprites;
using AVATron.Infrastructure.Audio;
using AVATron.Infrastructure.Persistence;

namespace AVATron.Avalonia.Shell;

/// Menu screens, drawn with the pixel font into the same framebuffer as the game.
internal sealed class Menus
{
    static readonly uint White = WilliamsPalette.Rgb(7, 7, 3), Yellow = WilliamsPalette.Rgb(7, 7, 0),
        Cyan = WilliamsPalette.Rgb(0, 7, 3), Red = WilliamsPalette.Rgb(7, 0, 0), Grey = WilliamsPalette.Rgb(4, 4, 2),
        Green = WilliamsPalette.Rgb(0, 7, 0), Orange = WilliamsPalette.Rgb(7, 2, 0);

    int _cursor, _scroll, _idleFrames, _attractPage;
    public Screen ReturnTo = Screen.Title;
    Screen _afterScores = Screen.Title;
    readonly char[] _initials = ['A', 'A', 'A'];
    int _initialPos;
    InputAction? _rebinding;
    readonly uint[] _pal = new uint[16];

    public void OnEnter(Screen s) { _cursor = 0; _scroll = 0; _idleFrames = 0; _rebinding = null; if (s == Screen.Title) _attractPage = 0; }

    public void BeginInitials() { _initials[0] = _initials[1] = _initials[2] = 'A'; _initialPos = 0; }

    public void ShowHighScores(AppController app, Screen after) { _afterScores = after; app.Go(Screen.HighScores); }

    // ======================================================================= update
    public void Update(AppController app)
    {
        var input = app.Input;
        _idleFrames++;
        var (dx, dy) = input.MenuNudge();
        bool ok = input.Pressed(InputAction.Start);
        bool back = input.BackPressed();
        if (dx != 0 || dy != 0 || ok || back) _idleFrames = 0;

        switch (app.Screen)
        {
            case Screen.Title:
            {
                // Attract cycle: title -> roster -> heroes, while idle.
                if (_idleFrames > 0 && _idleFrames % 480 == 0) _attractPage = (_attractPage + 1) % 3;
                if (dx != 0 || dy != 0 || ok || back) { if (_attractPage != 0) { _attractPage = 0; return; } }
                var items = TitleItems(app);
                Navigate(app, dy, items.Count);
                if (dx != 0 && items[_cursor].Id == "mode") ToggleMode(app);
                if (dx != 0 && items[_cursor].Id == "wave") { app.AdjustStartWave(dx); app.PlayUi(Cue.MenuMove); }
                if (ok) Activate(app, items[_cursor].Id);
                if (input.Pressed(InputAction.Help)) { ReturnTo = Screen.Title; app.Go(Screen.Help); }
                break;
            }
            case Screen.Paused:
            {
                var items = PauseItems(app);
                Navigate(app, dy, items.Count);
                if (back) app.Resume();
                else if (ok) Activate(app, items[_cursor].Id);
                break;
            }
            case Screen.Help:
                if (ok || back || input.Pressed(InputAction.Help)) { if (ReturnTo == Screen.Playing) app.Pause(); else app.Go(ReturnTo); }
                break;
            case Screen.HighScores:
                if (ok || back || _idleFrames > 600) app.Go(_afterScores);
                break;
            case Screen.EnterInitials:
                if (dy != 0) { _initials[_initialPos] = Cycle(_initials[_initialPos], -dy); app.PlayUi(Cue.MenuMove); }
                if (dx > 0 || (ok && _initialPos < 2)) { if (_initialPos < 2) _initialPos++; else app.SubmitInitials(new string(_initials)); }
                else if (ok) app.SubmitInitials(new string(_initials));
                else if (dx < 0 && _initialPos > 0) _initialPos--;
                break;
            case Screen.Settings:
            {
                var items = SettingItems(app);
                Navigate(app, dy, items.Count);
                if (back) { app.SaveSettings(); app.Go(Screen.Title); return; }
                var it = items[_cursor];
                if (dx != 0 && it.Adjust is not null) { it.Adjust(dx); app.ApplySettings(); app.PlayUi(Cue.MenuMove); }
                if (ok) { if (it.Activate is not null) it.Activate(); else it.Adjust?.Invoke(1); app.ApplySettings(); }
                break;
            }
            case Screen.Rebind:
            {
                if (_rebinding is not null) break;   // waiting for a key (handled in TryCaptureRebind)
                int n = Rebindable.Length + 2;
                Navigate(app, dy, n);
                if (back) { app.SaveSettings(); app.Go(Screen.Settings); return; }
                if (ok)
                {
                    if (_cursor < Rebindable.Length) _rebinding = Rebindable[_cursor];
                    else if (_cursor == Rebindable.Length) { app.Settings.KeyBindings = SettingsDocument.DefaultKeyBindings(); app.ApplySettings(); }
                    else { app.SaveSettings(); app.Go(Screen.Settings); }
                }
                break;
            }
        }
    }

    void Navigate(AppController app, int dy, int count)
    {
        if (dy == 0 || count == 0) return;
        _cursor = (_cursor + dy + count) % count;
        app.PlayUi(Cue.MenuMove);
    }

    static char Cycle(char c, int dir)
    {
        const string set = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789. ";
        int i = set.IndexOf(c); if (i < 0) i = 0;
        return set[(i + dir + set.Length) % set.Length];
    }

    public bool TryTypeInitial(Key key)
    {
        char? c = key is >= Key.A and <= Key.Z ? (char)('A' + (key - Key.A)) : key is >= Key.D0 and <= Key.D9 ? (char)('0' + (key - Key.D0)) : null;
        if (c is not null) { _initials[_initialPos] = c.Value; if (_initialPos < 2) _initialPos++; return true; }
        if (key == Key.Back) { if (_initialPos > 0) _initialPos--; return true; }
        return false;
    }

    public bool TryCaptureRebind(Key key)
    {
        if (_rebinding is not { } action) return false;
        _rebinding = null;
        if (key == Key.Escape) return true;   // cancel; Escape stays reserved for Pause/Back
        return RebindTarget is { } app && Bind(app, action, key);
    }

    internal AppController? RebindTarget;

    static bool Bind(AppController app, InputAction action, Key key)
    {
        var name = key.ToString();
        // A key drives one action: remove it elsewhere first.
        foreach (var list in app.Settings.KeyBindings.Values) list.RemoveAll(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
        app.Settings.KeyBindings[action] = [name];
        app.ApplySettings();
        return true;
    }

    void ToggleMode(AppController app)
    {
        app.Settings.Preset = app.IsModern ? PlayPreset.Classic : PlayPreset.Modern;
        app.ApplySettings(); app.SaveSettings(); app.LoadHighScores();
        app.PlayUi(Cue.MenuMove);
    }

    void Activate(AppController app, string id)
    {
        app.PlayUi(Cue.MenuSelect);
        switch (id)
        {
            case "start": app.StartGame(); break;
            case "resume-suspended": app.ResumeSuspended(); break;
            case "mode": ToggleMode(app); break;
            case "wave": app.StartGame(); break;
            case "scores": ShowHighScores(app, Screen.Title); break;
            case "settings": app.Go(Screen.Settings); break;
            case "help": ReturnTo = app.Screen; app.Go(Screen.Help); break;
            case "quit": app.Quit(); break;
            case "resume": app.Resume(); break;
            case "suspend": app.SuspendAndQuit(); break;
            case "to-title": app.QuitToTitle(); break;
        }
    }

    record MenuItem(string Id, string Label);

    static List<MenuItem> TitleItems(AppController app)
    {
        var l = new List<MenuItem> { new("start", "START GAME") };
        if (app.SuspendAvailable) l.Add(new("resume-suspended", "RESUME SUSPENDED GAME"));
        l.Add(new("mode", $"MODE: < {(app.IsModern ? "MODERN" : "CLASSIC")} >"));
        l.Add(new("wave", $"START WAVE: < {app.Settings.StartWave} >"));
        l.Add(new("scores", "HIGH SCORES"));
        l.Add(new("settings", "SETTINGS"));
        l.Add(new("help", "CONTROLS"));
        l.Add(new("quit", "QUIT"));
        return l;
    }

    static List<MenuItem> PauseItems(AppController app)
    {
        var l = new List<MenuItem> { new("resume", "RESUME"), new("help", "CONTROLS") };
        if (app.IsModern) l.Add(new("suspend", "SUSPEND AND QUIT"));
        l.Add(new("to-title", "QUIT TO TITLE"));
        return l;
    }

    sealed record Setting(string Label, Func<string> Value, Action<int>? Adjust = null, Action? Activate = null, bool ModernOnly = false);

    List<Setting> SettingItems(AppController app)
    {
        var s = app.Settings;
        string Pct(float v) => $"{(int)Math.Round(v * 100)}%";
        float Step(float v, int d, float lo = 0, float hi = 1, float st = 0.1f) => (float)Math.Round(Math.Clamp(v + d * st, lo, hi), 2);
        string OnOff(bool b) => b ? "ON" : "OFF";
        return
        [
            new("MODE", () => app.IsModern ? "MODERN" : "CLASSIC", _ => ToggleMode(app)),
            new("DIFFICULTY (ARCADE 3)", () => s.Difficulty.ToString(), d => s.Difficulty = Math.Clamp(s.Difficulty + d, 0, 10)),
            new("MEN PER GAME (ARCADE 3)", () => s.LivesPerGame.ToString(), d => s.LivesPerGame = Math.Clamp(s.LivesPerGame + d, 1, 20)),
            new("EXTRA MAN EVERY", () => s.ExtraLifeEvery == 0 ? "NEVER" : s.ExtraLifeEvery.ToString(), d => s.ExtraLifeEvery = Math.Clamp(s.ExtraLifeEvery + d * 5000, 0, 50000)),
            new("SOUND", () => OnOff(s.AudioEnabled), _ => s.AudioEnabled = !s.AudioEnabled),
            new("MASTER VOLUME", () => Pct(s.MasterVolume), d => s.MasterVolume = Step(s.MasterVolume, d)),
            new("EFFECTS VOLUME", () => Pct(s.EffectsVolume), d => s.EffectsVolume = Step(s.EffectsVolume, d)),
            new("AMBIENT VOLUME", () => Pct(s.AmbientVolume), d => s.AmbientVolume = Step(s.AmbientVolume, d)),
            new("AMBIENT HUM (ADDED)", () => OnOff(s.AmbientHum), _ => s.AmbientHum = !s.AmbientHum),
            new("FULLSCREEN (F11)", () => OnOff(s.Fullscreen), _ => { s.Fullscreen = !s.Fullscreen; FullscreenToggle?.Invoke(s.Fullscreen); }),
            new("INTEGER SCALING", () => OnOff(s.IntegerScaling), _ => s.IntegerScaling = !s.IntegerScaling),
            new("PIXEL SHAPE", () => s.SquarePixels ? "SQUARE" : "ARCADE 4:3", _ => s.SquarePixels = !s.SquarePixels),
            new("REDUCED MOTION", () => OnOff(s.ReducedMotion), _ => s.ReducedMotion = !s.ReducedMotion),
            new("SMOOTH SCALING", () => OnOff(s.SmoothScalingInModern), _ => s.SmoothScalingInModern = !s.SmoothScalingInModern, ModernOnly: true),
            new("SUPPRESS FLICKER", () => OnOff(s.SuppressFlickerInModern), _ => s.SuppressFlickerInModern = !s.SuppressFlickerInModern, ModernOnly: true),
            new("WAVE PROGRESS", () => OnOff(s.ShowWaveProgressInModern), _ => s.ShowWaveProgressInModern = !s.ShowWaveProgressInModern, ModernOnly: true),
            new("GAME SPEED", () => Pct(s.ModernGameSpeed), d => s.ModernGameSpeed = Step(s.ModernGameSpeed, d, 0.5f, 1f), ModernOnly: true),
            new("STICK DEADZONE", () => Pct(s.GamepadDeadzone), d => s.GamepadDeadzone = Step(s.GamepadDeadzone, d, 0.05f, 0.6f, 0.05f)),
            new("SAVE SLOT", () => s.SaveSlot.ToString(), d => { s.SaveSlot = (s.SaveSlot - 1 + d + 3) % 3 + 1; app.LoadHighScores(); }),
            new("REMAP KEYS...", () => "", Activate: () => { RebindTarget = app; app.Go(Screen.Rebind); }),
            new("RESTORE DEFAULTS", () => "", Activate: app.ResetSettings),
            new("BACK", () => "", Activate: () => { app.SaveSettings(); app.Go(Screen.Title); }),
        ];
    }

    public Action<bool>? FullscreenToggle;

    static readonly InputAction[] Rebindable =
    [
        InputAction.MoveUp, InputAction.MoveDown, InputAction.MoveLeft, InputAction.MoveRight,
        InputAction.FireUp, InputAction.FireDown, InputAction.FireLeft, InputAction.FireRight,
        InputAction.Start, InputAction.Pause, InputAction.Fullscreen, InputAction.Help,
    ];

    // ======================================================================= draw
    public void Draw(AppController app, FrameBuffer fb, long frame)
    {
        WilliamsPalette.ForFrame(_pal, frame, app.Settings.ReducedMotion);
        switch (app.Screen)
        {
            case Screen.Title:
                if (_attractPage == 1) DrawRoster(fb, frame);
                else if (_attractPage == 2) DrawScores(app, fb, "HALL OF HEROES");
                else DrawTitle(app, fb, frame);
                break;
            case Screen.HighScores: DrawScores(app, fb, app.IsModern ? "HEROES - MODERN" : "HEROES - CLASSIC"); break;
            case Screen.Settings: DrawSettings(app, fb); break;
            case Screen.Rebind: DrawRebind(app, fb); break;
            case Screen.Help: DrawHelp(app, fb); break;
            default: DrawTitle(app, fb, frame); break;
        }
    }

    /// Overlays drawn on top of the frozen game picture.
    public void DrawOverlay(AppController app, FrameBuffer fb)
    {
        switch (app.Screen)
        {
            case Screen.Paused:
            {
                var items = PauseItems(app);
                Box(fb, 76, 80, 140, 22 + items.Count * 11);
                fb.TextCentered("PAUSED", 86, Yellow);
                for (int i = 0; i < items.Count; i++) Item(fb, items[i].Label, 100 + i * 11, i == _cursor);
                break;
            }
            case Screen.Help: Box(fb, 10, 20, 272, 200); DrawHelpBody(app, fb, 26); break;
            case Screen.EnterInitials:
            {
                Box(fb, 36, 70, 220, 90);
                fb.TextCentered("YOU ARE AN AVATRON HERO", 78, Yellow);
                fb.TextCentered("ENTER YOUR INITIALS", 90, White);
                for (int i = 0; i < 3; i++)
                {
                    int x = 118 + i * 20;
                    fb.Text(_initials[i].ToString(), x, 108, i == _initialPos ? _pal[Pal.Cycle1] : White, 2);
                    if (i == _initialPos) fb.FillRect(x, 124, 10, 2, _pal[Pal.Cycle1]);
                }
                fb.TextCentered("TYPE, OR UP/DOWN TO CHANGE", 134, Grey);
                fb.TextCentered("RIGHT OR ENTER TO LOCK IN", 144, Grey);
                break;
            }
        }
    }

    void DrawTitle(AppController app, FrameBuffer fb, long frame)
    {
        fb.TextCentered(Branding.Title, 22, _pal[Pal.Cycle1], 2);
        fb.TextCentered(Branding.Tagline, 42, Cyan);
        var items = TitleItems(app);
        for (int i = 0; i < items.Count; i++) Item(fb, items[i].Label, 66 + i * 12, i == _cursor);
        string modeLine = app.Settings.StartWave > 1 ? $"PRACTICE FROM WAVE {app.Settings.StartWave}: NO HIGH SCORES"
            : app.IsModern ? "ANALOG AIM, NO FLICKER, PAUSE+SUSPEND" : "1982 ARCADE RULES, 8-WAY STICKS";
        fb.TextCentered(modeLine, 66 + items.Count * 12 + 6, Grey);
        fb.TextCentered($"HIGH SCORE {app.HighScores.TopScore}", 170, Yellow);
        fb.TextCentered("WASD MOVE   ARROWS FIRE   F1 HELP", 184, Green);
        fb.TextCentered(Branding.Disclaimer1, 208, Grey);
        fb.TextCentered(Branding.Disclaimer2, 217, Grey);
        fb.TextCentered(Branding.Disclaimer3, 226, Grey);
    }

    void DrawRoster(FrameBuffer fb, long frame)
    {
        fb.TextCentered("MEET THE ROBOTS", 12, Yellow);
        var rows = new (Sprite s, string name, string pts)[]
        {
            (SpriteLibrary.Grunt[(int)(frame / 10) % 3], "GRUNT", "100"),
            (SpriteLibrary.Hulk[(int)(frame / 10) % 3], "HULK", "INDESTRUCTIBLE"),
            (SpriteLibrary.Brain[(int)(frame / 10) % 2], "BRAIN", "500"),
            (SpriteLibrary.ProgFromDaddy[0], "PROG", "100"),
            (SpriteLibrary.Sphereoid[(int)(frame / 4) % 8], "SPHEREOID", "1000"),
            (SpriteLibrary.Enforcer, "ENFORCER", "150"),
            (SpriteLibrary.Quark[(int)(frame / 4) % 4], "QUARK", "1000"),
            (SpriteLibrary.Tank[(int)(frame / 6) % 2], "TANK", "200"),
            (SpriteLibrary.Electrode[0], "ELECTRODE", "AVOID"),
            (SpriteLibrary.CruiseMissile, "MISSILE SPARK SHELL", "25"),
        };
        for (int i = 0; i < rows.Length; i++)
        {
            int y = 28 + i * 19;
            fb.Sprite(rows[i].s, 50 - rows[i].s.Width / 2, y, _pal);
            fb.Text(rows[i].name, 80, y + 4, White);
            fb.Text(rows[i].pts, 200, y + 4, Cyan);
        }
        fb.TextCentered("RESCUE: 1000 2000 3000 4000 5000", 222, Green);
    }

    void DrawScores(AppController app, FrameBuffer fb, string title)
    {
        fb.TextCentered(title, 16, Yellow, 2);
        var e = app.HighScores.Entries;
        if (e.Count == 0) fb.TextCentered("NO SCORES YET", 100, Grey);
        for (int i = 0; i < e.Count; i++)
        {
            uint c = i == app.LastRank ? _pal[Pal.Cycle1] : i == 0 ? Yellow : White;
            int y = 48 + i * 15;
            fb.Text($"{i + 1,2}.", 50, y, c);
            fb.Text(e[i].Initials, 76, y, c);
            fb.Text(e[i].Score.ToString().PadLeft(8), 110, y, c);
            fb.Text($"W{e[i].Wave}", 170, y, Grey);
            fb.Text(e[i].Date.ToString("yyyy-MM-dd"), 196, y, Grey);
        }
        fb.TextCentered($"SLOT {app.Settings.SaveSlot}", 214, Grey);
    }

    void DrawSettings(AppController app, FrameBuffer fb)
    {
        fb.TextCentered("SETTINGS", 6, Yellow);
        var items = SettingItems(app);
        const int visible = 19, top = 20, lh = 10;
        if (_cursor < _scroll) _scroll = _cursor;
        if (_cursor >= _scroll + visible) _scroll = _cursor - visible + 1;
        for (int i = _scroll; i < Math.Min(items.Count, _scroll + visible); i++)
        {
            var it = items[i];
            int y = top + (i - _scroll) * lh;
            bool sel = i == _cursor;
            bool inactive = it.ModernOnly && !app.IsModern;
            uint c = sel ? _pal[Pal.Cycle1] : inactive ? Grey : White;
            fb.Text((sel ? ">" : " ") + it.Label, 8, y, c);
            string v = it.Value();
            if (v.Length > 0) fb.Text(v, 284 - PixelFont.Measure(v), y, sel ? Yellow : inactive ? Grey : Cyan);
        }
        string hint = items[_cursor].ModernOnly && !app.IsModern ? "APPLIES IN MODERN MODE ONLY" : "LEFT/RIGHT CHANGE  ENTER SELECT  ESC BACK";
        fb.TextCentered(hint, 216, Grey);
        fb.TextCentered(app.AudioStatus.ToUpperInvariant(), 226, Grey);
    }

    void DrawRebind(AppController app, FrameBuffer fb)
    {
        fb.TextCentered("REMAP KEYS", 8, Yellow);
        for (int i = 0; i < Rebindable.Length + 2; i++)
        {
            int y = 26 + i * 13;
            bool sel = i == _cursor;
            uint c = sel ? _pal[Pal.Cycle1] : White;
            if (i < Rebindable.Length)
            {
                var a = Rebindable[i];
                fb.Text((sel ? ">" : " ") + Pretty(a), 20, y, c);
                string keys = _rebinding == a ? "PRESS A KEY..." : string.Join(" ", app.Settings.KeyBindings.GetValueOrDefault(a) ?? []).ToUpperInvariant();
                fb.Text(keys, 170, y, _rebinding == a ? Yellow : Cyan);
            }
            else fb.Text((sel ? ">" : " ") + (i == Rebindable.Length ? "RESET TO DEFAULTS" : "BACK"), 20, y, c);
        }
        fb.TextCentered("ENTER: REBIND   ESC: CANCEL/BACK", 222, Grey);
    }

    static string Pretty(InputAction a) => a switch
    {
        InputAction.MoveUp => "MOVE UP", InputAction.MoveDown => "MOVE DOWN", InputAction.MoveLeft => "MOVE LEFT", InputAction.MoveRight => "MOVE RIGHT",
        InputAction.FireUp => "FIRE UP", InputAction.FireDown => "FIRE DOWN", InputAction.FireLeft => "FIRE LEFT", InputAction.FireRight => "FIRE RIGHT",
        InputAction.Start => "START/CONFIRM", InputAction.Pause => "PAUSE/BACK", InputAction.Fullscreen => "FULLSCREEN", _ => "HELP",
    };

    void DrawHelp(AppController app, FrameBuffer fb) => DrawHelpBody(app, fb, 12);

    void DrawHelpBody(AppController app, FrameBuffer fb, int y)
    {
        string K(InputAction a) => string.Join("/", app.Settings.KeyBindings.GetValueOrDefault(a) ?? []).ToUpperInvariant();
        fb.TextCentered("CONTROLS", y, Yellow); y += 14;
        fb.Text($"MOVE    {K(InputAction.MoveUp)} {K(InputAction.MoveLeft)} {K(InputAction.MoveDown)} {K(InputAction.MoveRight)}", 20, y, White); y += 10;
        fb.Text("FIRE    ARROW KEYS (HOLD TO KEEP FIRING)", 20, y, White); y += 10;
        fb.Text($"PAUSE   {K(InputAction.Pause)}      HELP {K(InputAction.Help)}", 20, y, White); y += 10;
        fb.Text($"FULLSCREEN {K(InputAction.Fullscreen)}", 20, y, White); y += 14;
        fb.Text("GAMEPAD: LEFT STICK MOVES,", 20, y, Cyan); y += 10;
        fb.Text("RIGHT STICK AIMS AND FIRES, START PAUSES", 20, y, Cyan); y += 14;
        fb.Text("SHOOT EVERY ROBOT EXCEPT HULKS -", 20, y, Green); y += 10;
        fb.Text("THEY CAN ONLY BE PUSHED BACK.", 20, y, Green); y += 10;
        fb.Text("TOUCH THE FAMILY TO RESCUE THEM.", 20, y, Green); y += 10;
        fb.Text("AVOID ELECTRODES. BRAINS TURN", 20, y, Green); y += 10;
        fb.Text("HUMANS INTO PROGS.", 20, y, Green); y += 14;
        fb.Text(app.IsModern ? "MODERN: ANALOG STICKS, PAUSE, SUSPEND" : "CLASSIC: 8-WAY STICKS AS IN 1982", 20, y, Orange); y += 14;
        fb.TextCentered("PRESS ENTER TO CONTINUE", y, Grey);
    }

    void Item(FrameBuffer fb, string label, int y, bool selected)
    {
        uint c = selected ? _pal[Pal.Cycle1] : White;
        fb.TextCentered(selected ? $"> {label} <" : label, y, c);
    }

    static void Box(FrameBuffer fb, int x, int y, int w, int h)
    {
        fb.FillRect(x, y, w, h, 0xFF000000);
        fb.Rect(x, y, w, h, WilliamsPalette.Rgb(0, 0, 3), 2);
    }
}
