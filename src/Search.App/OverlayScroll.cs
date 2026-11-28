using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Search.App;

/// <summary>
/// The app's own scrollbars as Zen's (the ScrollViewer and ScrollBar styles in App.xaml draw them over the content):
/// hidden until the list scrolls or the pointer comes near the bar, gone again after a second of nothing,
/// and "hot" (wider, a step in from the edge) under the pointer or while dragged.
/// </summary>
public static class OverlayScroll
{
    public static readonly DependencyProperty IsHotProperty =
        DependencyProperty.RegisterAttached("IsHot", typeof(bool), typeof(OverlayScroll), new PropertyMetadata(false));
    public static bool GetIsHot(DependencyObject o) => (bool)o.GetValue(IsHotProperty);
    public static void SetIsHot(DependencyObject o, bool v) => o.SetValue(IsHotProperty, v);

    const double Near = 18;
    static readonly TimeSpan Idle = TimeSpan.FromMilliseconds(1000);
    static readonly ConditionalWeakTable<ScrollViewer, DispatcherTimer> timers = new();

    public static void Register()
    {
        EventManager.RegisterClassHandler(typeof(ScrollViewer), ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnScrolled), true);
        EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.MouseMoveEvent, new MouseEventHandler(OnMove), true);
        EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.MouseEnterEvent, new MouseEventHandler((s, _) => Show((ScrollViewer)s)), true);
        EventManager.RegisterClassHandler(typeof(ScrollBar), UIElement.MouseEnterEvent, new MouseEventHandler((s, _) => Heat((ScrollBar)s)), true);
        EventManager.RegisterClassHandler(typeof(ScrollBar), UIElement.MouseLeaveEvent, new MouseEventHandler((s, _) => Heat((ScrollBar)s)), true);
        EventManager.RegisterClassHandler(typeof(ScrollBar), Mouse.GotMouseCaptureEvent, new MouseEventHandler((s, _) => Heat((ScrollBar)s)), true);
        EventManager.RegisterClassHandler(typeof(ScrollBar), Mouse.LostMouseCaptureEvent, new MouseEventHandler((s, _) => Heat((ScrollBar)s)), true);
    }

    static void Heat(ScrollBar bar) => SetIsHot(bar, bar.IsMouseOver || bar.IsMouseCaptureWithin);

    static void OnScrolled(object sender, ScrollChangedEventArgs e)
    {
        // Only the viewer's own scrolling: a list inside it raises the same event on the way up.
        if (e.OriginalSource != sender) return;
        if (e.VerticalChange != 0 || e.HorizontalChange != 0) Show((ScrollViewer)sender);
    }

    static void OnMove(object sender, MouseEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        var p = e.GetPosition(sv);
        if (p.X >= sv.ActualWidth - Near || p.Y >= sv.ActualHeight - Near) Show(sv);
    }

    static (ScrollBar? V, ScrollBar? H) Bars(ScrollViewer sv) =>
        (sv.Template?.FindName("PART_VerticalScrollBar", sv) as ScrollBar, sv.Template?.FindName("PART_HorizontalScrollBar", sv) as ScrollBar);

    static void Show(ScrollViewer sv)
    {
        var (v, h) = Bars(sv);
        var any = false;
        foreach (var bar in new[] { v, h })
        {
            if (bar is null || bar.Visibility != Visibility.Visible) continue;
            any = true;
            bar.IsHitTestVisible = true;
            if (bar.Opacity < 1 && !Equals(bar.Tag, "showing"))
            {
                bar.Tag = "showing";
                bar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)));
                App.Trace($"overlay bar shown in {Name(sv)}");
            }
        }
        if (!any) return;
        if (!timers.TryGetValue(sv, out var timer))
        {
            timer = new DispatcherTimer { Interval = Idle };
            timer.Tick += (_, _) => Fade(sv, timer);
            timers.Add(sv, timer);
        }
        timer.Stop();
        timer.Start();
    }

    static void Fade(ScrollViewer sv, DispatcherTimer timer)
    {
        var (v, h) = Bars(sv);
        if (v?.IsMouseOver == true || h?.IsMouseOver == true || v?.IsMouseCaptureWithin == true || h?.IsMouseCaptureWithin == true) return; // keeps ticking
        timer.Stop();
        foreach (var bar in new[] { v, h })
        {
            if (bar is null || !Equals(bar.Tag, "showing")) continue;
            bar.Tag = null;
            bar.IsHitTestVisible = false;
            bar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)));
            App.Trace($"overlay bar hidden in {Name(sv)}");
        }
    }

    static string Name(ScrollViewer sv)
    {
        for (DependencyObject? o = sv; o is not null; o = System.Windows.Media.VisualTreeHelper.GetParent(o))
            if (o is FrameworkElement f && !string.IsNullOrEmpty(f.Name)) return f.Name;
        return "a list";
    }
}
