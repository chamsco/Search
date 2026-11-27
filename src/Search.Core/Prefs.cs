namespace Search.Core;

public enum LookChoice { System, Light, Dark }
public enum TabsLayout { Sidebar, Top }
public enum AskSite { Claude, ChatGPT, Perplexity }

/// <summary>Ask in the empty tab's field: the words go to an AI site in a tab; nothing is built into Search.</summary>
public static class AskSites
{
    public static string Url(AskSite site, string words)
    {
        var q = Uri.EscapeDataString(words.Trim());
        return site switch
        {
            AskSite.ChatGPT => "https://chatgpt.com/?q=" + q,
            AskSite.Perplexity => "https://www.perplexity.ai/search?q=" + q,
            _ => "https://claude.ai/new?q=" + q,
        };
    }
}

/// <summary>Settings. Port of the ones that apply on Windows from Sources/Search/Prefs.swift; kept in prefs.json.</summary>
public sealed class PrefsData
{
    public Engine Engine { get; set; } = Engines.Standard;
    public string CustomEngine { get; set; } = "";
    /// <summary>Settings › General › Ask uses: where the empty tab's Ask mode sends the question.</summary>
    public AskSite Ask { get; set; } = AskSite.Claude;
    /// <summary>Settings › General › Suggestions from your search engine.</summary>
    public bool EngineSuggests { get; set; } = true;
    public LookChoice Look { get; set; } = LookChoice.System;
    public bool BookmarksBar { get; set; }
    /// <summary>Settings › Privacy › Upgrade http:// pages to HTTPS where the site has it.</summary>
    public bool HttpsUpgrade { get; set; } = true;
    /// <summary>Where downloads go. Empty: the Downloads folder.</summary>
    public string DownloadFolder { get; set; } = "";
    /// <summary>Settings › General › Start with a fresh window instead of last time's tabs.</summary>
    public bool StartFresh { get; set; }
    public bool ShowAddressOnTab { get; set; }
    public double SideWidth { get; set; } = 232;
    /// <summary>Settings › Tabs › Spaces: off until turned on, as on the Mac; until then there is one space and nothing about it shows.</summary>
    public bool SpacesOn { get; set; } = true;
    /// <summary>The Library section last shown in the column.</summary>
    public string LibrarySection { get; set; } = "spaces";
    /// <summary>Settings › Tabs › Layout: the column of tabs at the side, or a strip along the top.</summary>
    public TabsLayout Layout { get; set; } = TabsLayout.Sidebar;
    /// <summary>Settings › Tabs › Pins as rows instead of squares.</summary>
    public bool PinsAsRows { get; set; }
    /// <summary>Settings › Tabs › Close tabs left alone: days unattended before Search closes a tab and archives it; 0 never.</summary>
    public int CloseIdleDays { get; set; }
    /// <summary>Settings › General › Little windows: a link from another app opens in a small window.</summary>
    public bool LittleLinks { get; set; }
    /// <summary>Settings › Keyboard &amp; Mouse › Peek: Shift+click a link to see its page over this one.</summary>
    public bool Peek { get; set; } = true;
    /// <summary>Settings › Keyboard &amp; Mouse › Split View: tabs side by side, up to four.</summary>
    public bool SplitView { get; set; } = true;
    /// <summary>The click that peeks: "shift", "altShift" or "altCtrl".</summary>
    public string PeekClick { get; set; } = "shift";
    /// <summary>A peek stays with the tab it came from: another tab hides it, and coming back shows it.</summary>
    public bool PeekStays { get; set; }
    /// <summary>A link from a pinned tab to another site opens in a peek, so the pin stays on its page.</summary>
    public bool PinsPeek { get; set; } = true;
    /// <summary>Alt+click on a link opens it beside the page, in Split View.</summary>
    public bool AltSplit { get; set; }
    /// <summary>Settings › Privacy › Block trackers: ad and tracking hosts are not asked at all.</summary>
    public bool BlockTrackers { get; set; }
    /// <summary>Settings › Privacy › Proxy: what Windows says, none, or a server of your own (host:port).</summary>
    public ProxyMode ProxyMode { get; set; } = ProxyMode.System;
    public string ProxyServer { get; set; } = "";
    /// <summary>The user name for a proxy that asks for one. The password is not here: it is kept protected for this Windows account (ProxyVault).</summary>
    public string ProxyUser { get; set; } = "";
    /// <summary>Settings › Passwords › Ask Windows Hello before filling: one of Search's own passwords goes into a page only once the person has said so. Off unless turned on.</summary>
    public bool AskHello { get; set; }
    /// <summary>Offer the details of Settings › Passwords › My card under name, email, phone and address boxes.</summary>
    public bool FillFromCard { get; set; }
    /// <summary>Settings › Keyboard &amp; Mouse › Type a site's name, press Tab, and search that site.</summary>
    public bool SiteSearch { get; set; } = true;
    /// <summary>Settings › Sites › Your own style for a site: off until turned on, as on the Mac.</summary>
    public bool SiteStylesOn { get; set; }
}

public sealed class Prefs
{
    const string FileName = "prefs.json";
    public PrefsData Data { get; private set; } = new();
    public event Action? Changed;

    public static Prefs Load() => new() { Data = Store.Read<PrefsData>(FileName) ?? new() };

    public void Update(Action<PrefsData> change)
    {
        change(Data);
        Store.Write(FileName, Data);
        Changed?.Invoke();
    }

    public string EffectiveDownloadFolder =>
        Data.DownloadFolder.Length > 0 && Directory.Exists(Data.DownloadFolder)
            ? Data.DownloadFolder
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
}
