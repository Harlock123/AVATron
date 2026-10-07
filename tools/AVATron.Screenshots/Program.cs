// Regenerates docs/images/*.png from the real renderer with fixed seeds, so screenshots are reproducible
// and never include anything from the desktop.   Usage: dotnet run --project tools/AVATron.Screenshots [outDir]
using Avalonia.Input;
using AVATron.Avalonia.Rendering;
using AVATron.Avalonia.Shell;
using AVATron.Core.Entities;
using AVATron.Core.Input;
using AVATron.Core.Scoring;
using AVATron.Core.Simulation;
using AVATron.Infrastructure.Audio;
using AVATron.Infrastructure.Input;
using AVATron.Infrastructure.Persistence;
using SkiaSharp;

string outDir = args.Length > 0 ? args[0] : Path.Combine("docs", "images");
Directory.CreateDirectory(outDir);
var fb = new FrameBuffer(GameRenderer.Width, GameRenderer.Height);

AppController NewApp(PlayPreset preset = PlayPreset.Classic, int startWave = 1, ulong seed = 2026)
{
    var dir = Path.Combine(Path.GetTempPath(), "avatron-shots-" + Guid.NewGuid().ToString("N"));
    var app = new AppController(new DataPaths(dir), new AudioMixer(), new NoGamepadSource("none"), "SDL2 audio", () => seed) { ShowPracticeNote = false };
    app.Settings.Preset = preset;
    app.Settings.StartWave = startWave;
    app.Settings.LivesPerGame = 5;
    app.ApplySettings();
    return app;
}

void Tap(AppController app, Key k) { app.KeyDown(k); app.Advance(1 / 60.0); app.KeyUp(k); app.Advance(1 / 60.0); }

// Crude deterministic autopilot so gameplay shots show action: fire at the nearest threat, sidestep when close.
TickInput Bot(GameSession g)
{
    var w = g.World;
    Entity? near = null; long best = long.MaxValue;
    foreach (var e in w.Enemies)
    {
        if (e.Dead || e.Kind == EntityKind.Hulk) continue;
        long dx = e.CenterX - w.Player.CenterX, dy = e.CenterY - w.Player.CenterY, d = dx * dx + dy * dy;
        if (d < best) { best = d; near = e; }
    }
    if (near is null) return TickInput.None;
    int fx = near.CenterX - w.Player.CenterX, fy = near.CenterY - w.Player.CenterY;
    var move = best < 45 * 45 ? StickQuantizer.EightWay(-fx, -fy, 0) : StickInput.Neutral;
    return new TickInput(move, StickQuantizer.EightWay(fx, fy, 0));
}

void Play(AppController app, int ticksAfterIntro)
{
    app.StartGame();
    var g = app.Session!;
    while (g.Phase == GamePhase.WaveIntro) app.StepSimulation(TickInput.None);
    for (int t = 0; t < ticksAfterIntro; t++) app.StepSimulation(Bot(g));
    // Don't capture mid-death or between waves.
    for (int t = 0; t < 400 && g.Phase != GamePhase.Playing; t++) app.StepSimulation(Bot(g));
}

void Save(AppController app, string name)
{
    app.Render(fb);
    // 3x scale with the arcade monitor's 4:3 shape: 292x240 -> 960x720, nearest neighbour.
    const int W = 960, H = 720;
    using var bmp = new SKBitmap(new SKImageInfo(W, H, SKColorType.Bgra8888, SKAlphaType.Premul));
    for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            uint p = fb.Pixels[(y * GameRenderer.Height / H) * fb.Width + x * GameRenderer.Width / W];
            bmp.SetPixel(x, y, new SKColor(p));
        }
    using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
    var path = Path.Combine(outDir, name + ".png");
    using (var f = File.Create(path)) data.SaveTo(f);
    Console.WriteLine($"wrote {path}");
}

{ var a = NewApp(); a.Advance(1 / 60.0); Save(a, "title"); }
{ var a = NewApp(); for (int i = 0; i < 485; i++) a.Advance(1 / 60.0); Save(a, "roster"); }
{ var a = NewApp(); Play(a, 200); Save(a, "wave1"); }
{
    var a = NewApp(startWave: 4); a.StartGame();
    for (int i = 0; i < 30; i++) a.StepSimulation(TickInput.None);   // mid materialisation
    Save(a, "wave-intro");
}
{ var a = NewApp(startWave: 5, seed: 7); Play(a, 700); Save(a, "brains"); }
{ var a = NewApp(startWave: 7, seed: 11); Play(a, 420); Save(a, "quark-wave"); }
{ var a = NewApp(startWave: 9, seed: 3); Play(a, 60); Save(a, "grunt-swarm"); }
{ var a = NewApp(PlayPreset.Modern, startWave: 6, seed: 5); Play(a, 500); Save(a, "modern-hud"); }
{
    var a = NewApp();
    string[] names = ["AVA", "TRN", "JAZ", "KIM", "LEO", "MAX", "NED", "OLI", "PIP", "ROX"];
    for (int i = 0; i < names.Length; i++)
        a.HighScores.Insert(new HighScoreEntry(names[i], 250_000 - i * 21_350, 40 - i * 3, new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero)));
    Tap(a, Key.Down); Tap(a, Key.Down); Tap(a, Key.Down); Tap(a, Key.Enter);   // HIGH SCORES
    Save(a, "high-scores");
}
{ var a = NewApp(); Tap(a, Key.Down); Tap(a, Key.Down); Tap(a, Key.Down); Tap(a, Key.Down); Tap(a, Key.Enter); Save(a, "settings"); }
