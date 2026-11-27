using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Search.Core;

namespace Search.App;

/// <summary>
/// A line in the column that is not a tab: the name above a tab group, or a folder among the pinned rows.
/// Port of the headings in TabGroups.swift and PinFolderViews.swift.
/// </summary>
public sealed class RowHeading : INotifyPropertyChanged
{
    public Guid Id { get; }
    public bool IsFolder { get; }
    public RowHeading(Guid id, bool isFolder) { Id = id; IsFolder = isFolder; }

    string name = "";
    public string Name { get => name; set { if (Set(ref name, value)) Raise(nameof(Letter)); } }
    public string Letter => name.Length == 0 ? "•" : char.ToUpperInvariant(name[0]).ToString();

    bool collapsed;
    public bool Collapsed { get => collapsed; set { if (Set(ref collapsed, value)) { Raise(nameof(Mark)); Raise(nameof(CountText)); } } }
    /// <summary>The fold mark: pointing right when folded, down when open.</summary>
    public string Mark => collapsed ? "" : "";

    int depth;
    public int Depth { get => depth; set { if (Set(ref depth, value)) { Raise(nameof(Indent)); } } }
    public System.Windows.Thickness Indent => new(depth * 12, 0, 0, 0);

    int? colour;
    public int? Colour { get => colour; set { if (Set(ref colour, value)) { Raise(nameof(Stripe)); Raise(nameof(Tint)); } } }
    public Brush? Stripe
    {
        get
        {
            if (colour is not int index || Spaces.Rgb(index) is not { } c) return null;
            var b = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
            b.Freeze();
            return b;
        }
    }

    /// <summary>The group's colour, softly, as the pill behind its name in the top strip.</summary>
    public Brush Tint
    {
        get
        {
            var c = colour is int index && Spaces.Rgb(index) is { } rgb ? Color.FromArgb(0x30, rgb.R, rgb.G, rgb.B) : Color.FromArgb(0x1F, 0x80, 0x80, 0x80);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }

    int count;
    /// <summary>How many tabs it holds, shown while it is folded.</summary>
    public int Count { get => count; set { if (Set(ref count, value)) Raise(nameof(CountText)); } }
    public string CountText => collapsed && count > 0 ? count.ToString() : "";

    bool editing;
    public bool Editing { get => editing; set => Set(ref editing, value); }

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    bool Set<T>(ref T field, T value, [CallerMemberName] string? p = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(p!);
        return true;
    }
}

/// <summary>Picks a tab's template or a heading's for one line of the column.</summary>
/// <summary>A post of a live folder, a line under its heading: it opens its page in a new tab, as a bookmark would.</summary>
public sealed class FeedRow
{
    public FeedRow(FeedItem item, int depth) { Item = item; Indent = new System.Windows.Thickness(depth * 12 + 14, 0, 0, 2); }
    public FeedItem Item { get; }
    public string Title => Item.Title;
    public System.Windows.Thickness Indent { get; }
}

public sealed class RowSelector : System.Windows.Controls.DataTemplateSelector
{
    readonly System.Windows.DataTemplate tab, heading, feed;
    public RowSelector(System.Windows.DataTemplate tab, System.Windows.DataTemplate heading, System.Windows.DataTemplate? feed = null) { this.tab = tab; this.heading = heading; this.feed = feed ?? heading; }
    public override System.Windows.DataTemplate SelectTemplate(object? item, System.Windows.DependencyObject container) => item is RowHeading ? heading : item is FeedRow ? feed : tab;
}
