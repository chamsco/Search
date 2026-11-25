using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Web.WebView2.Core;
using Search.Core;

namespace Search.App;

/// <summary>The proxy password, the one secret in Settings › Privacy › Proxy. Kept protected for this Windows account (DPAPI) in its own file, read only when a proxy asks for it, never sent to a page (Proxy.swift's ProxySecret).</summary>
public static class ProxyVault
{
    static string FilePath => Store.File("proxy-secret.bin");
    public static bool Has => File.Exists(FilePath);

    public static void Write(string password)
    {
        if (password.Length == 0) { Forget(); return; }
        try { File.WriteAllBytes(FilePath, ProtectedData.Protect(Encoding.UTF8.GetBytes(password), null, DataProtectionScope.CurrentUser)); }
        catch (Exception e) { App.Log(e); }
    }

    public static string Read()
    {
        try { return Has ? Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(FilePath), null, DataProtectionScope.CurrentUser)) : ""; }
        catch { return ""; }
    }

    public static void Forget() { try { File.Delete(FilePath); } catch { } }
}

public partial class MainWindow
{
    /// <summary>A proxy of the person's own that asks who they are: the name from Settings and the password from the vault, answered for the proxy only (never for a site that asks).</summary>
    void OnProxyAuth(CoreWebView2BasicAuthenticationRequestedEventArgs e)
    {
        var d = Shared.Prefs.Data;
        if (d.ProxyMode != ProxyMode.Custom || d.ProxyUser.Length == 0) return;
        if (Proxy.Normalise(d.ProxyServer) is not { Length: > 0 } server) return;
        var hostPort = server.Contains("://") ? server[(server.IndexOf("://", StringComparison.Ordinal) + 3)..] : server;
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var from) || !string.Equals($"{from.Host}:{from.Port}", hostPort, StringComparison.OrdinalIgnoreCase)) return;
        e.Response.UserName = d.ProxyUser;
        e.Response.Password = ProxyVault.Read();
        App.Trace("proxy sign-in answered for " + hostPort);
    }
}
