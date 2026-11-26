using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Search.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Search.App;

/// <summary>
/// A small window around one page: the little window a link from another app opens in, and the panel of a Peek.
/// A thin bar says where it is, with Open in Search and close.
/// </summary>
public sealed class MiniWindow : Window
{
    public WebView2 View { get; } = new() { DefaultBackgroundColor = System.Drawing.Color.White };
    public Button OpenInSearch { get; } = Pill("Open in Search");
    public Button Shut { get; } = Pill("✕");
    readonly TextBlock site = new() { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 12, Margin = new Thickness(12, 0, 8, 0) };
    readonly bool little;

    public MiniWindow(bool little)
    {
        this.little = little;
        WindowStyle = WindowStyle.None;
        ResizeMode = little ? ResizeMode.CanResize : ResizeMode.NoResize;
        ShowInTaskbar = little;
        ShowActivated = true;
        Title = "Search";
        MinWidth = 280; MinHeight = 240;
        WindowChrome.SetWindowChrome(this, new WindowChrome { CaptionHeight = little ? 36 : 0, ResizeBorderThickness = new Thickness(little ? 5 : 0), GlassFrameThickness = new Thickness(0), CornerRadius = new CornerRadius(0), UseAeroCaptionButtons = false });
        SetResourceReference(BackgroundProperty, "Ground");
        site.SetResourceReference(TextBlock.ForegroundProperty, "Ink");

        var bar = new Grid { Height = 36 };
        bar.ColumnDefinitions.Add(new ColumnDefinition());
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        bar.Children.Add(site);
        Grid.SetColumn(OpenInSearch, 1); bar.Children.Add(OpenInSearch);
        Grid.SetColumn(Shut, 2); bar.Children.Add(Shut);
        Shut.Margin = new Thickness(0, 0, 6, 0);
        System.Windows.Automation.AutomationProperties.SetName(Shut, "Close small window");
        System.Windows.Automation.AutomationProperties.SetAutomationId(Shut, "MiniClose");
        OpenInSearch.Margin = new Thickness(0, 0, 4, 0);
        bar.SetResourceReference(Panel.BackgroundProperty, "Wash");
        View.SetValue(Grid.RowProperty, 1);

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.Children.Add(bar);
        grid.Children.Add(View);
        var frame = new Border { BorderThickness = new Thickness(1), Child = grid };
        frame.SetResourceReference(Border.BorderBrushProperty, "Hairline");
        Content = frame;

        SourceInitialized += (_, _) => Round();
        SizeChanged += (_, _) => Round();
    }

    static Button Pill(string text)
    {
        var b = new Button { Content = text, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(0), BorderThickness = new Thickness(0), Background = Brushes.Transparent, Cursor = Cursors.Hand, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        b.SetResourceReference(Control.ForegroundProperty, "Ink");
        WindowChrome.SetIsHitTestVisibleInChrome(b, true);
        return b;
    }

    public void Say(string title, string address)
    {
        site.Text = string.IsNullOrWhiteSpace(title) ? address : $"{title}  ·  {Address.Host(address)}";
        if (little) Title = string.IsNullOrWhiteSpace(title) ? "Search" : title + " - Search";
    }

    void Round()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var scale = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1;
        Native.Round(hwnd, (int)(ActualWidth * scale), (int)(ActualHeight * scale), (int)(10 * scale));
    }
}
