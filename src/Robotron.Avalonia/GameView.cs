using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Robotron.Avalonia.Rendering;
using Robotron.Avalonia.Shell;

namespace Robotron.Avalonia;

/// The single drawing surface. Each animation frame: advance the controller by real time, draw the
/// framebuffer, copy it into a WriteableBitmap, and scale it to the window (letterboxed).
public sealed class GameView : Control
{
    readonly AppController _app;
    readonly FrameBuffer _fb = new(GameRenderer.Width, GameRenderer.Height);
    readonly WriteableBitmap _bitmap = new(new PixelSize(GameRenderer.Width, GameRenderer.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
    readonly Stopwatch _clock = Stopwatch.StartNew();
    double _last;
    bool _attached;

    public GameView(AppController app) { _app = app; Focusable = true; ClipToBounds = true; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _last = _clock.Elapsed.TotalSeconds;
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        base.OnDetachedFromVisualTree(e);
    }

    void OnFrame(TimeSpan _)
    {
        if (!_attached) return;
        double now = _clock.Elapsed.TotalSeconds;
        double dt = Math.Min(0.1, now - _last);
        _last = now;
        _app.Advance(dt);
        _app.Render(_fb);
        using (var l = _bitmap.Lock())
        {
            unsafe
            {
                for (int y = 0; y < _fb.Height; y++)
                    _fb.Pixels.AsSpan(y * _fb.Width, _fb.Width).CopyTo(new Span<uint>((byte*)l.Address + y * l.RowBytes, _fb.Width));
            }
        }
        InvalidateVisual();
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
    }

    /// Destination rectangle in DIPs. Integer scaling is computed in device pixels so HiDPI stays sharp.
    public static Rect ComputeDestination(Size bounds, double renderScaling, bool squarePixels, bool integerScaling)
    {
        double devW = bounds.Width * renderScaling, devH = bounds.Height * renderScaling;
        // Arcade monitor: 292x240 shown on a 4:3 tube, so each pixel is (4/3)/(292/240) wide.
        double pixelAspect = squarePixels ? 1.0 : (4.0 / 3.0) / ((double)GameRenderer.Width / GameRenderer.Height);
        double sy = Math.Min(devH / GameRenderer.Height, devW / (GameRenderer.Width * pixelAspect));
        if (integerScaling && sy >= 1) sy = Math.Floor(sy);
        double sx = squarePixels ? sy : sy * pixelAspect;
        double w = GameRenderer.Width * sx, h = GameRenderer.Height * sy;
        double x = Math.Floor((devW - w) / 2), y = Math.Floor((devH - h) / 2);
        return new Rect(x / renderScaling, y / renderScaling, w / renderScaling, h / renderScaling);
    }

    public override void Render(DrawingContext ctx)
    {
        ctx.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
        var s = _app.Settings;
        double scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var dest = ComputeDestination(Bounds.Size, scaling, s.SquarePixels, s.IntegerScaling);
        bool smooth = _app.IsModern && s.SmoothScalingInModern;
        using (ctx.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = smooth ? BitmapInterpolationMode.HighQuality : BitmapInterpolationMode.None }))
            ctx.DrawImage(_bitmap, new Rect(0, 0, GameRenderer.Width, GameRenderer.Height), dest);
    }
}
