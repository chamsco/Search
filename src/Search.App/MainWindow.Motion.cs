using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Search.App;

/// <summary>
/// Motion (Motion.swift): the column folds away and back, a space swipes its rows in from the side it lies on,
/// and a tab switch dips the page for a moment. All of it off when Windows is set to show no animations.
/// </summary>
public partial class MainWindow
{
    static bool Moves => SystemParameters.ClientAreaAnimation;

    /// <summary>A column's width carried from one value to another, easing out.</summary>
    void FoldColumn(double from, double to, Action done)
    {
        if (!Moves) { SideColumn.Width = new GridLength(to); done(); return; }
        var start = DateTime.UtcNow;
        const double ms = 200;
        EventHandler? step = null;
        step = (_, _) =>
        {
            var t = Math.Min(1, (DateTime.UtcNow - start).TotalMilliseconds / ms);
            var eased = 1 - Math.Pow(1 - t, 3);
            SideColumn.Width = new GridLength(from + (to - from) * eased);
            if (t < 1) return;
            CompositionTarget.Rendering -= step;
            SideColumn.Width = new GridLength(to);
            done();
        };
        CompositionTarget.Rendering += step;
    }

    /// <summary>The rows come in from the side the space lies on: from the right when it is after the one left, from the left when before.</summary>
    void SwipeRows(int direction)
    {
        if (!Moves || direction == 0) return;
        foreach (UIElement el in new UIElement[] { PinGrid, TabList, TopTabs })
        {
            var move = new TranslateTransform(direction * 36, 0);
            el.RenderTransform = move;
            move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(direction * 36, 0, TimeSpan.FromMilliseconds(230)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            el.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
        }
    }

    Popup? dip;

    /// <summary>The page behind a tab switch is covered with the page's ground and uncovers, so one page does not just snap to the next. (A web view is a window of its own and cannot fade, so a pane of the ground fades over it.)</summary>
    void DipPage()
    {
        if (!Moves || !IsVisible || WindowState == WindowState.Minimized || Stage.ActualWidth < 60) return;
        try
        {
            dip ??= new Popup
            {
                AllowsTransparency = true, StaysOpen = true, Placement = PlacementMode.Relative, PlacementTarget = Card, IsHitTestVisible = false,
                Child = new Border { IsHitTestVisible = false, CornerRadius = new CornerRadius(10) },
            };
            var cover = (Border)dip.Child;
            cover.SetResourceReference(Border.BackgroundProperty, "Ground");
            cover.Width = Card.ActualWidth;
            cover.Height = Card.ActualHeight;
            dip.IsOpen = true;
            var fade = new DoubleAnimation(0.7, 0, TimeSpan.FromMilliseconds(170));
            fade.Completed += (_, _) => { if (dip is { } d) d.IsOpen = false; };
            cover.BeginAnimation(UIElement.OpacityProperty, fade);
        }
        catch (Exception e) { App.Log(e); }
    }
}
