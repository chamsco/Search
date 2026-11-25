namespace Search.Core;

public enum PermissionKind { Camera, Microphone, Location, Notifications, Clipboard, Sensors, Other }
public enum PermissionChoice { Allow, Block }

/// <summary>
/// What you told each site it may use (Settings › Privacy › Site permissions; the Mac keeps the same in Permissions.swift).
/// A site is its origin: scheme, host and port. A choice with nothing remembered means "ask".
/// </summary>
public sealed class SitePermissions
{
    const string FileName = "permissions.json";

    public Dictionary<string, Dictionary<string, PermissionChoice>> Sites { get; set; } = [];

    public event Action? Changed;

    public static SitePermissions Load() => Store.Read<SitePermissions>(FileName) ?? new();

    /// <summary>"https://example.com:8443" from any address on it; empty for something that is not a site.</summary>
    public static string OriginOf(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Scheme is "http" or "https" ? u.GetLeftPart(UriPartial.Authority).ToLowerInvariant() : "";

    public PermissionChoice? Get(string origin, PermissionKind kind) =>
        Sites.TryGetValue(origin, out var own) && own.TryGetValue(kind.ToString(), out var c) ? c : null;

    public void Set(string origin, PermissionKind kind, PermissionChoice choice)
    {
        if (origin.Length == 0) return;
        if (!Sites.TryGetValue(origin, out var own)) Sites[origin] = own = [];
        own[kind.ToString()] = choice;
        Save();
    }

    /// <summary>Forget one choice: the site asks again next time.</summary>
    public void Reset(string origin, PermissionKind kind)
    {
        if (!Sites.TryGetValue(origin, out var own) || !own.Remove(kind.ToString())) return;
        if (own.Count == 0) Sites.Remove(origin);
        Save();
    }

    public void ResetSite(string origin) { if (Sites.Remove(origin)) Save(); }
    public void ResetAll() { if (Sites.Count == 0) return; Sites.Clear(); Save(); }

    public IEnumerable<(string Origin, PermissionKind Kind, PermissionChoice Choice)> All() =>
        Sites.OrderBy(s => s.Key, StringComparer.Ordinal)
             .SelectMany(s => s.Value.Select(p => (s.Key, Enum.TryParse<PermissionKind>(p.Key, out var k) ? k : PermissionKind.Other, p.Value)));

    public static string Describe(PermissionKind kind) => kind switch
    {
        PermissionKind.Camera => "use your camera",
        PermissionKind.Microphone => "use your microphone",
        PermissionKind.Location => "know your location",
        PermissionKind.Notifications => "show notifications",
        PermissionKind.Clipboard => "see what you copy",
        PermissionKind.Sensors => "use motion sensors",
        _ => "use a device feature",
    };

    void Save() { Store.Write(FileName, this); Changed?.Invoke(); }
}
