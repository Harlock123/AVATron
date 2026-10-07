using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Robotron.Avalonia;

/// Validation spike: fixed 60 Hz simulation, render to 292x240 framebuffer via Skia, nearest-neighbour scale.
public sealed class SpikeView : Control
{
    const int W = 292, H = 240;
    readonly WriteableBitmap _fb = new(new PixelSize(W, H), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
    readonly HashSet<Key> _held = [];
    readonly Stopwatch _clock = Stopwatch.StartNew();
    double _acc, _last; long _ticks; int _frames; double _fpsT; string _fps = "";
    float _x = 140, _y = 110;

    public SpikeView() { Focusable = true; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Focus();
        TopLevel.GetTopLevel(this)!.RequestAnimationFrame(OnFrame);
    }
    protected override void OnKeyDown(KeyEventArgs e) { _held.Add(e.Key); e.Handled = true; }
    protected override void OnKeyUp(KeyEventArgs e) { _held.Remove(e.Key); e.Handled = true; }

    void OnFrame(TimeSpan _)
    {
        double now = _clock.Elapsed.TotalSeconds;
        _acc += Math.Min(0.25, now - _last); _last = now;
        while (_acc >= 1 / 60.0) { Step(); _acc -= 1 / 60.0; }
        _frames++; if (now - _fpsT >= 1) { _fps = $"{_frames} fps / {_ticks} ticks"; _frames = 0; _fpsT = now; Console.WriteLine(_fps); }
        Paint();
        InvalidateVisual();
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
    }

    void Step()
    {
        _ticks++;
        if (_held.Contains(Key.A)) _x -= 1; if (_held.Contains(Key.D)) _x += 1;
        if (_held.Contains(Key.W)) _y -= 1; if (_held.Contains(Key.S)) _y += 1;
        _x = Math.Clamp(_x, 0, W - 10); _y = Math.Clamp(_y, 0, H - 10);
    }

    void Paint()
    {
        using var l = _fb.Lock();
        var info = new SKImageInfo(W, H, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surf = SKSurface.Create(info, l.Address, l.RowBytes);
        var c = surf.Canvas;
        c.Clear(SKColors.Black);
        using var p = new SKPaint { Color = new SKColor(255, 64, 64) };
        c.DrawRect(_x, _y, 10, 10, p);
        p.Color = SKColors.Cyan; p.Style = SKPaintStyle.Stroke;
        c.DrawRect(2.5f, 2.5f, W - 5, H - 5, p);
    }

    public override void Render(DrawingContext ctx)
    {
        double s = Math.Min(Bounds.Width / W, Bounds.Height / H);
        var dest = new Rect((Bounds.Width - W * s) / 2, (Bounds.Height - H * s) / 2, W * s, H * s);
        using (ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            ctx.DrawImage(_fb, new Rect(0, 0, W, H), dest);
    }
}
