using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Search.App;

/// <summary>
/// Every colour is a pair, one for a light window and one for a dark.
/// Values are lifted from Sources/Search/Design.swift (Palette.NS and Greys.frame).
/// Brushes live in Application.Resources under these keys and are swapped
/// when the look changes, so every view that uses DynamicResource follows.
/// </summary>
public static class Palette
{
    public enum Look { System, Light, Dark }

    static Color White(double v) { var b = (byte)Math.Round(v * 255); return Color.FromRgb(b, b, b); }
    static Color Shade(bool dark, double light, double darkAlpha) =>
        dark ? Color.FromArgb((byte)Math.Round(darkAlpha * 255), 255, 255, 255)
             : Color.FromArgb((byte)Math.Round(light * 255), 0, 0, 0);

    public static bool IsDark { get; private set; }

    public static bool SystemIsDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return false; }
    }

    public static void Apply(ResourceDictionary r, Look look)
    {
        var dark = look switch { Look.Dark => true, Look.Light => false, _ => SystemIsDark() };
        IsDark = dark;
        Color P(double l, double d) => White(dark ? d : l);
        void Set(string key, Color c) { var b = new SolidColorBrush(c); b.Freeze(); r[key] = b; r[key + ".Color"] = c; }

        Set("Ground", P(1.0, 0.11));
        Set("Ink", P(0.09, 0.93));
        Set("Muted", P(0.55, 0.58));
        Set("Faint", P(0.83, 0.32));
        Set("Hairline", P(0.91, 0.20));
        Set("Wash", P(0.937, 0.175));
        Set("Hover", P(0.965, 0.15));
        Set("Frame", P(0.955, 0.075));
        Set("Card.Deep", P(0.925, 0.085));
        Set("Safe", dark ? Color.FromRgb(74, 222, 128) : Color.FromRgb(20, 128, 61));
        Set("Unsafe", dark ? Color.FromRgb(250, 191, 36) : Color.FromRgb(181, 84, 10));

        // Greys.frame: the column's rows, pins and buttons on the window's ground beside the card.
        Set("Row.Wash", Shade(dark, 0.0628, 0.0865));
        Set("Row.Hover", Shade(dark, 0.0419, 0.0595));
        Set("Row.Tile", Shade(dark, 0.0314, 0.0432));
        Set("Row.PinLive", Shade(dark, 0.0995, 0.1243));
        Set("Row.Muted", Shade(dark, 0.4241, 0.5459));
        Set("Row.Faint", Shade(dark, 0.1309, 0.2649));
        Set("Mark.Fill", dark ? Color.FromArgb(15, 237, 237, 237) : Color.FromArgb(15, 23, 23, 23));
        Set("Close.Hover", Color.FromRgb(196, 43, 28));
    }
}
