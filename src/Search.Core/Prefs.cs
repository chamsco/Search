namespace Search.Core;

public enum LookChoice { System, Light, Dark }
public enum TabsLayout { Sidebar, Top }

/// <summary>Settings. Port of the ones that apply on Windows from Sources/Search/Prefs.swift; kept in prefs.json.</summary>
public sealed class PrefsData
{
    public Engine Engine { get; set; } = Engines.Standard;
    public string CustomEngine { get; set; } = "";
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
    public bool SpacesOn { get; set; }
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
