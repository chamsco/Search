namespace Search.Core;

public enum LookChoice { System, Light, Dark }

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
