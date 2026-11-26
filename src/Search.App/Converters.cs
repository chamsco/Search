using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Search.App;

public sealed class VisibleWhen : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class VisibleUnless : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c) => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>The site's own icon for a row under the field, when Search has seen it; searches wear the engine's. Nothing when it has not (the row keeps its glyph).</summary>
public sealed class SuggestionIcon : IValueConverter
{
    public object? Convert(object value, Type t, object p, CultureInfo c) => value is Search.Core.Suggestion s
        ? MainWindow.IconOf(s.Kind is Search.Core.SuggestionKind.Search or Search.Core.SuggestionKind.Suggested ? "https://www.google.com/" : s.Url)
        : null;
    public object ConvertBack(object value, Type t, object p, CultureInfo c) => throw new NotSupportedException();
}
