using Avalonia.Input;
using AVATron.Avalonia.Input;
using AVATron.Avalonia.Rendering;
using AVATron.Core.Input;
using AVATron.Core.Scoring;
using AVATron.Core.Simulation;
using AVATron.Core.Waves;
using AVATron.Infrastructure.Audio;
using AVATron.Infrastructure.Input;
using AVATron.Infrastructure.Persistence;

namespace AVATron.Avalonia.Shell;

public enum Screen { Title, Playing, Paused, EnterInitials, HighScores, Settings, Rebind, Help }

/// Application flow and policy wiring. Owns settings, high scores, the current GameSession, audio dispatch and
/// menus. Has no window: the view feeds it keys/time and blits what it draws, so it runs headlessly in tests.
public sealed class AppController
{
    public AppController(DataPaths paths, AudioMixer mixer, IGamepadSource gamepad, string audioStatus, Func<ulong>? seeds = null)
    {
        _paths = paths; _mixer = mixer; _gamepad = gamepad; AudioStatus = audioStatus;
        _seeds = seeds ?? (() => (ulong)DateTime.UtcNow.Ticks ^ (ulong)Environment.ProcessId << 32);
        var r = SettingsDocument.Store().Load(paths.SettingsFile);
        Settings = r.Value;
        Settings.EnsureAllBindings();
        if (r.Message is not null) Notice = r.Message;
        Input = new InputMapper(Settings.KeyBindings);
        _table = WaveTable.LoadDefault();
        _sounds = SoundBank.Build();
        _menus = new Menus { FullscreenToggle = b => FullscreenRequested?.Invoke(b) };
        ApplySettings();
        LoadHighScores();
    }

    readonly DataPaths _paths;
    readonly AudioMixer _mixer;
    readonly IGamepadSource _gamepad;
    readonly Func<ulong> _seeds;
    readonly WaveTable _table;
    readonly SoundBank _sounds;
    readonly GameRenderer _renderer = new();
    readonly FrameSnapshot _snapshot = new();
    readonly Menus _menus;

    public SettingsDocument Settings { get; private set; }
    public InputMapper Input { get; }
    public Screen Screen { get; private set; } = Screen.Title;
    public GameSession? Session { get; private set; }
    public HighScoreTable HighScores { get; private set; } = new();
    public string AudioStatus { get; }
    public string? Notice { get; private set; }
    public bool IsModern => Settings.Preset == PlayPreset.Modern;
    public bool SuspendAvailable => IsModern && File.Exists(_paths.SuspendFile(Settings.SaveSlot, "modern"));

    public event Action<bool>? FullscreenRequested;
    public event Action? QuitRequested;
    public event Action? SettingsChanged;

    double _accumulator;
    int _noticeFrames;
    long _uiFrames;
    internal int LastRank = -1;

    // ------------------------------------------------------------------ input entry points
    public void KeyDown(Key key)
    {
        if (Screen == Screen.Rebind && _menus.TryCaptureRebind(key)) return;
        if (Screen == Screen.EnterInitials && _menus.TryTypeInitial(key)) return;
        Input.KeyDown(key);
    }
    public void KeyUp(Key key) => Input.KeyUp(key);
    public void FocusLost() => Input.ReleaseAll();

    // ------------------------------------------------------------------ per UI frame
    /// Advance by real elapsed time. Simulation runs at the arcade's 60.096 Hz (times the Modern speed setting),
    /// at most 5 ticks per call so a stall cannot snowball.
    public void Advance(double seconds)
    {
        _uiFrames++;
        Input.UpdateGamepad(_gamepad.Poll());
        if (_noticeFrames > 0 && --_noticeFrames == 0) Notice = null;

        if (Input.Pressed(InputAction.Fullscreen)) { Settings.Fullscreen = !Settings.Fullscreen; FullscreenRequested?.Invoke(Settings.Fullscreen); SaveSettings(); }

        switch (Screen)
        {
            case Screen.Playing: UpdatePlaying(seconds); break;
            default: _menus.Update(this); break;
        }
        Input.EndFrame();
    }

    void UpdatePlaying(double seconds)
    {
        if (Input.Pressed(InputAction.Pause)) { Pause(); return; }
        if (Input.Pressed(InputAction.Help)) { _menus.ReturnTo = Screen.Playing; Go(Screen.Help); _mixer.StopEffects(); return; }
        double speed = IsModern ? Math.Clamp(Settings.ModernGameSpeed, 0.25f, 1f) : 1.0;
        _accumulator += seconds * Arena.FrameRate * speed;
        int ticks = 0;
        while (_accumulator >= 1 && ticks < 5)
        {
            _accumulator -= 1; ticks++;
            StepSimulation(Input.BuildTickInput());
            if (Screen != Screen.Playing) return;
        }
        if (ticks == 5) _accumulator = 0;
    }

    /// One simulation tick plus its consequences (audio, game-over flow). Public for tests.
    public void StepSimulation(TickInput input)
    {
        var g = Session!;
        g.Tick(input);
        foreach (var e in g.Events)
        {
            var cue = SoundBank.CueFor(e);
            if (cue is not null) _mixer.Play(_sounds[cue.Value]);
        }
        if (g.Phase == GamePhase.Finished) GameFinished();
    }

    // ------------------------------------------------------------------ flow
    public void StartGame()
    {
        var rules = (IsModern ? GameRules.Modern : GameRules.Classic) with
        {
            Difficulty = Settings.Difficulty, LivesPerGame = Settings.LivesPerGame, ExtraLifeEvery = Settings.ExtraLifeEvery,
        };
        Session = new GameSession(rules, _table, new XorShiftRandom(_seeds()));
        _accumulator = 0;
        _mixer.StopEffects();
        _mixer.Play(_sounds[Cue.GameStart]);
        SetAmbient(true);
        Go(Screen.Playing);
    }

    public void Pause()
    {
        if (Session is null) return;
        _mixer.StopEffects();
        SetAmbient(false);
        Go(Screen.Paused);
    }

    public void Resume()
    {
        if (Session is null) { Go(Screen.Title); return; }
        _accumulator = 0;
        SetAmbient(true);
        Go(Screen.Playing);
    }

    public void QuitToTitle()
    {
        Session = null;
        SetAmbient(false);
        _mixer.StopEffects();
        Go(Screen.Title);
    }

    void GameFinished()
    {
        var g = Session!;
        SetAmbient(false);
        if (HighScores.Qualifies(g.Score)) { _menus.BeginInitials(); Go(Screen.EnterInitials); }
        else { LastRank = -1; Session = null; _menus.ShowHighScores(this, Screen.Title); }
    }

    public void SubmitInitials(string initials)
    {
        if (Session is null) return;
        LastRank = HighScores.Insert(new HighScoreEntry(initials, Session.Score, Session.Wave, DateTimeOffset.Now));
        SaveHighScores();
        Session = null;
        _menus.ShowHighScores(this, Screen.Title);
    }

    /// Modern only: write the suspend file and leave to the title screen.
    public bool SuspendAndQuit()
    {
        if (!IsModern || Session is null || Session.Phase is GamePhase.GameOver or GamePhase.Finished) return false;
        try
        {
            var doc = new SuspendDocument { Saved = DateTimeOffset.Now, State = Session.CreateSuspendState(), StateHash = Session.StateHash() };
            SuspendDocument.Store().Save(_paths.SuspendFile(Settings.SaveSlot, "modern"), doc);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowNotice($"COULD NOT SAVE: {ex.Message}");
            return false;
        }
        QuitToTitle();
        ShowNotice("GAME SUSPENDED");
        return true;
    }

    public bool ResumeSuspended()
    {
        var path = _paths.SuspendFile(Settings.SaveSlot, "modern");
        var r = SuspendDocument.Store().Load(path);
        if (r.UsedDefaults || r.Value.State is null)
        {
            ShowNotice(r.Message ?? "NO SUSPENDED GAME");
            return false;
        }
        try { File.Delete(path); } catch (IOException) { }
        Session = GameSession.Resume(r.Value.State, _table, new XorShiftRandom(1));
        if (Session.StateHash() != r.Value.StateHash) ShowNotice("RESUMED (STATE CHECK MISMATCH)");
        Pause();   // resume into the pause menu so the player is ready
        return true;
    }

    /// Called when the window is closing: Modern games are suspended automatically.
    public void OnShutdown()
    {
        if (IsModern && Session is not null && Screen is Screen.Playing or Screen.Paused or Screen.Help) SuspendAndQuit();
        SaveSettings();
    }

    public void Quit() => QuitRequested?.Invoke();
    internal void Go(Screen s) { Screen = s; _menus.OnEnter(s); }

    internal void PlayUi(Cue c) => _mixer.Play(_sounds[c]);

    void SetAmbient(bool on) => _mixer.SetAmbient(on && Settings.AmbientHum ? _sounds[Cue.AmbientHum] : null);

    public void ShowNotice(string text) { Notice = text; _noticeFrames = 240; }

    // ------------------------------------------------------------------ settings & persistence
    public void ApplySettings()
    {
        _mixer.MasterVolume = Settings.MasterVolume;
        _mixer.EffectsVolume = Settings.EffectsVolume;
        _mixer.AmbientVolume = Settings.AmbientVolume;
        _mixer.Muted = !Settings.AudioEnabled;
        _mixer.Mode = IsModern ? MixMode.Polyphonic : MixMode.SingleChannelPriority;
        Input.SetBindings(Settings.KeyBindings);
        Input.Deadzone = Settings.GamepadDeadzone;
        Input.AnalogMove = IsModern;
        Input.AnalogFire = IsModern;
        SettingsChanged?.Invoke();
    }

    public void SaveSettings()
    {
        try { SettingsDocument.Store().Save(_paths.SettingsFile, Settings); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { ShowNotice($"COULD NOT SAVE SETTINGS: {ex.Message}"); }
    }

    public void ResetSettings()
    {
        Settings = new SettingsDocument { Fullscreen = Settings.Fullscreen };
        ApplySettings(); SaveSettings(); LoadHighScores();
    }

    public void LoadHighScores()
    {
        var r = HighScoreDocument.Store().Load(_paths.HighScoreFile(Settings.SaveSlot, Settings.Preset.ToString()));
        HighScores = r.Value.ToTable();
        if (r.Message is not null) ShowNotice(r.Message);
    }

    void SaveHighScores()
    {
        try { HighScoreDocument.Store().Save(_paths.HighScoreFile(Settings.SaveSlot, Settings.Preset.ToString()), HighScoreDocument.FromTable(HighScores)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { ShowNotice($"COULD NOT SAVE HIGH SCORES: {ex.Message}"); }
    }

    // ------------------------------------------------------------------ drawing
    public void Render(FrameBuffer fb)
    {
        if (Session is not null && Screen is Screen.Playing or Screen.Paused or Screen.Help or Screen.EnterInitials)
        {
            FrameSnapshot.Fill(Session, _snapshot);
            _snapshot.HighScore = Math.Max(HighScores.TopScore, Session.Score);
            string? note = IsModern && Settings.ModernGameSpeed < 1f ? $"SPEED {Settings.ModernGameSpeed * 100:0}%" : null;
            _renderer.Render(_snapshot, fb, new GameRenderOptions
            {
                Flicker = !IsModern || !Settings.SuppressFlickerInModern,
                ReducedMotion = Settings.ReducedMotion,
                ModernHud = IsModern,
                WaveProgress = IsModern && Settings.ShowWaveProgressInModern,
                StatusNote = note,
            });
            if (Screen != Screen.Playing) _menus.DrawOverlay(this, fb);
        }
        else
        {
            fb.Clear();
            _menus.Draw(this, fb, _uiFrames);
        }
        if (Notice is not null) fb.TextCentered(Notice, GameRenderer.Height - 9, WilliamsPalette.Rgb(7, 7, 0));
    }
}
