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

    Guid? groupId;
    /// <summary>The tab group it is in.</summary>
    public Guid? GroupId { get => groupId; set { if (Set(ref groupId, value)) Raise(nameof(InGroup)); } }
    public bool InGroup => groupId is not null;

    System.Windows.Media.Brush? groupBrush;
    /// <summary>The colour of its group's mark under the tab in the top strip (null outside a group).</summary>
    public System.Windows.Media.Brush? GroupBrush { get => groupBrush; set => Set(ref groupBrush, value); }
    /// <summary>For a pin, the folder among the pinned rows it is in.</summary>
    public Guid? FolderId { get; set; }

    System.Windows.Thickness indent = new(0, 0, 0, 2);
    /// <summary>How far in the row is set: inside a group, or a pinned folder.</summary>
    public System.Windows.Thickness Indent { get => indent; set { if (Set(ref indent, value)) Raise(nameof(RowMargin)); } }

    bool splitDrop;
    /// <summary>A tab being dragged is over this one, to be put beside it in Split View when let go.</summary>
    public bool SplitDrop { get => splitDrop; set => Set(ref splitDrop, value); }

    /// <summary>The empty page put beside another by Split with New Tab: until something is typed or picked, the new-tab field shows in its place.</summary>
    public bool Fresh { get; set; }
    public bool ShowsField => Fresh && url == "about:blank";

    bool picked;
    /// <summary>One of several tabs chosen at once (Ctrl+click, Shift+click), for the menu to act on together.</summary>
    public bool Picked { get => picked; set => Set(ref picked, value); }

    bool inSplit;
    /// <summary>Shown beside others in Split View.</summary>
    public bool InSplit { get => inSplit; set => Set(ref inSplit, value); }

    int splitPos;
    /// <summary>Where the row sits in its split's shared box in the column: 0 not in one, 1 first, 2 middle, 3 last.</summary>
    public int SplitPos { get => splitPos; set { if (Set(ref splitPos, value)) Raise(nameof(RowMargin)); } }

    /// <summary>The row's margin: the indent, without the gap below where the next row of the same split box follows.</summary>
    public System.Windows.Thickness RowMargin => splitPos is 1 or 2 ? new(indent.Left, indent.Top, indent.Right, 0) : indent;

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
        Url = url, Title = title, PinId = pinId, Home = Home, Name = name, LastSeen = LastSeen, Group = groupId, Folder = FolderId,
    };

    public static Tab From(SessionEntry e) => new()
    {
        url = e.Url, title = e.Title, pinId = e.PinId, Home = e.Home, name = e.Name, LastSeen = e.LastSeen ?? DateTime.UtcNow, groupId = e.Group, FolderId = e.Folder,
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
