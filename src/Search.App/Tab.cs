using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using Search.Core;

namespace Search.App;

/// <summary>
/// One page in the column. Port of the parts of Sources/Search/Tab.swift that
/// milestone 1 needs. A tab restored from last time holds no web view until it
/// is first shown (asleep), as on the Mac, so a long session opens instantly.
/// </summary>
public sealed class Tab : INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();
    /// <summary>The space whose row it is in; the engine's store for its pages follows.</summary>
    public Guid SpaceId { get; set; }

    string url = "";
    public string Url { get => url; set { if (Set(ref url, value)) { Raise(nameof(IsBlank)); Raise(nameof(Label)); Raise(nameof(Monogram)); Raise(nameof(Glyph)); Raise(nameof(IsInternal)); Raise(nameof(HasGlyph)); Raise(nameof(ShowsLetter)); } } }

    string title = "";
    public string Title { get => title; set { if (Set(ref title, value)) Raise(nameof(Label)); } }

    string? name;
    /// <summary>The name you gave the tab, when you gave it one.</summary>
    public string? Name { get => name; set { if (Set(ref name, value)) Raise(nameof(Label)); } }

    ImageSource? icon;
    public ImageSource? Icon { get => icon; set { if (Set(ref icon, value)) { Raise(nameof(HasIcon)); Raise(nameof(ShowsLetter)); } } }
    public bool HasIcon => icon is not null;

    bool loading;
    public bool Loading { get => loading; set => Set(ref loading, value); }

    bool live;
    public bool Live { get => live; set => Set(ref live, value); }

    bool editing;
    public bool Editing { get => editing; set => Set(ref editing, value); }

    Guid? pinId;
    public Guid? PinId { get => pinId; set { if (Set(ref pinId, value)) Raise(nameof(IsPin)); } }
    public bool IsPin => pinId is not null;

    /// <summary>A pin's own page, the one it was pinned at.</summary>
    public string? Home { get; set; }

    public bool CanGoBack { get => canGoBack; set => Set(ref canGoBack, value); }
    bool canGoBack;
    public bool CanGoForward { get => canGoForward; set => Set(ref canGoForward, value); }
    bool canGoForward;

    public WebView2? View { get; set; }
    public bool Asleep => View is null;

    public bool IsBlank => string.IsNullOrEmpty(url);

    /// <summary>One of Search's own pages (search://settings and the like), which wears a glyph instead of a site's icon.</summary>
    public string? Glyph => InternalPages.Parse(url) is { } p ? InternalPages.Glyph(p.Kind) : null;
    public bool IsInternal => Glyph is not null;
    public bool HasGlyph => IsInternal;
    public bool ShowsLetter => !HasIcon && !IsInternal;

    /// <summary>Set while a http:// address is being tried over https, so a failure can fall back to it.</summary>
    public string? UpgradedFrom { get; set; }
    /// <summary>An http:// address that is to load as written, because its https:// didn't answer.</summary>
    public string? NoUpgrade { get; set; }
    public double Zoom { get; set; } = 1;

    /// <summary>What the row says: your name for it, the page's title, or the address until the page says.</summary>
    public string Label =>
        !string.IsNullOrWhiteSpace(name) ? name! :
        IsBlank ? "New Tab" :
        InternalPages.Parse(url) is { } page ? InternalPages.Title(page.Kind) :
        !string.IsNullOrWhiteSpace(title) ? title :
        Address.Pretty(url);

    public string Monogram
    {
        get
        {
            var host = Address.Host(url);
            return string.IsNullOrEmpty(host) ? "•" : char.ToUpperInvariant(host[0]).ToString();
        }
    }

    /// <summary>When it was last the page on screen.</summary>
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    public SessionEntry ToEntry() => new()
    {
        Url = url, Title = title, PinId = pinId, Home = Home, Name = name, LastSeen = LastSeen,
    };

    public static Tab From(SessionEntry e) => new()
    {
        url = e.Url, title = e.Title, pinId = e.PinId, Home = e.Home, name = e.Name, LastSeen = e.LastSeen ?? DateTime.UtcNow,
    };

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
