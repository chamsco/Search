using System.Windows;
using System.Windows.Media;

namespace Search.App;

/// <summary>
/// A ball of dots turning slowly, drawn in the page's ink: the empty tab's mark.
/// Dots on a sphere, the near ones larger and darker, the far side faint; it turns about its upright axis and nods a little.
/// </summary>
public sealed class DotBall : FrameworkElement
{
    const int Dots = 520;
    static readonly (double X, double Y, double Z)[] Points = Make();

    double turn;
    DateTime last;
    bool running;

    public bool Still { get; set; }

    public DotBall()
    {
        IsHitTestVisible = false;
        Loaded += (_, _) => Start();
        Unloaded += (_, _) => Stop();
        IsVisibleChanged += (_, _) => { if (IsVisible) Start(); else Stop(); };
    }

    static (double, double, double)[] Make()
    {
        // A Fibonacci sphere: even dots with no poles bunched up.
        var points = new (double, double, double)[Dots];
        var golden = Math.PI * (3 - Math.Sqrt(5));
        for (var i = 0; i < Dots; i++)
        {
            var y = 1 - 2.0 * (i + 0.5) / Dots;
            var r = Math.Sqrt(1 - y * y);
            var a = golden * i;
            points[i] = (Math.Cos(a) * r, y, Math.Sin(a) * r);
        }
        return points;
    }

    void Start()
    {
        if (running || !IsLoaded || !IsVisible) return;
        running = true;
        last = DateTime.UtcNow;
        CompositionTarget.Rendering += Frame;
    }

    void Stop()
    {
        if (!running) return;
        running = false;
        CompositionTarget.Rendering -= Frame;
    }

    void Frame(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        var dt = Math.Min(0.1, (now - last).TotalSeconds);
        if (dt < 0.028) return; // about 30 frames a second is plenty for dots
        last = now;
        if (!Still) turn += dt * 0.55;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= 0) return;
        var radius = size / 2 - 3;
        var centre = new Point(ActualWidth / 2, ActualHeight / 2);
        var ink = TryFindResource("Ink.Color") is Color c ? c : Colors.Black;
        var tilt = 0.32 + (Still ? 0 : Math.Sin(turn * 0.5) * 0.07);
        var (sinT, cosT) = Math.SinCos(turn);
        var (sinN, cosN) = Math.SinCos(tilt);
        var brushes = new SolidColorBrush[8];
        for (var i = 0; i < brushes.Length; i++)
        {
            brushes[i] = new SolidColorBrush(Color.FromArgb((byte)(40 + i * 30), ink.R, ink.G, ink.B));
            brushes[i].Freeze();
        }
        foreach (var (x0, y0, z0) in Points)
        {
            // About the upright axis, then nodded toward the viewer.
            var x = x0 * cosT + z0 * sinT;
            var z1 = -x0 * sinT + z0 * cosT;
            var y = y0 * cosN - z1 * sinN;
            var z = y0 * sinN + z1 * cosN;
            var depth = (z + 1) / 2; // 0 far .. 1 near
            var shade = Math.Clamp((int)(depth * brushes.Length), 0, brushes.Length - 1);
            // The top of the ball is lit: a little more weight toward the upper half, as in a dotted halftone.
            var lit = 0.75 + 0.25 * ((-y + 1) / 2);
            var r = (0.55 + depth * 1.15) * lit * (size / 120);
            dc.DrawEllipse(brushes[shade], null, new Point(centre.X + x * radius, centre.Y + y * radius), r, r);
        }
    }
}
