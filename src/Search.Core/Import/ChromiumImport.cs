using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Search.Core.Import;

/// <summary>Chrome, Edge, Brave, Arc (Windows): a Chromium profile folder. Bookmarks are a JSON file, history and sign-ins are SQLite; the passwords are sealed with AES-GCM under a key that Windows protects for the account (Local State), which the app unwraps.</summary>
public static class ChromiumImport
{
    // MARK: - bookmarks

    public static List<ImportedBookmark> Bookmarks(string json)
    {
        var found = new List<ImportedBookmark>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("roots", out var roots)) return found;
            foreach (var root in roots.EnumerateObject())
                if (root.Value.ValueKind == JsonValueKind.Object) Walk(root.Value, null, found, top: true);
        }
        catch (JsonException) { }
        return found;
    }

    static void Walk(JsonElement node, string? folder, List<ImportedBookmark> into, bool top)
    {
        var type = node.TryGetProperty("type", out var t) ? t.GetString() : null;
        var name = node.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
        if (type == "url")
        {
            var url = node.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                into.Add(new ImportedBookmark(name, url, folder));
            return;
        }
        if (!node.TryGetProperty("children", out var kids)) return;
        // A root (Bookmarks bar, Other bookmarks) holds its bookmarks at the top; a folder inside gives its name to what it holds.
        var here = top ? null : name.Length > 0 ? name : folder;
        foreach (var kid in kids.EnumerateArray()) Walk(kid, here, into, top: false);
    }

    // MARK: - history

    /// <summary>Chromium keeps time as microseconds since 1601.</summary>
    public static DateTime FromChromeTime(long micros) =>
        micros <= 0 ? DateTime.UtcNow : DateTime.UnixEpoch.AddSeconds(micros / 1_000_000.0 - 11_644_473_600.0);

    public static List<ImportedVisit> History(string dbFile, int limit = 20000)
    {
        var rows = new List<ImportedVisit>();
        WithCopy(dbFile, copy =>
        {
            using var c = Open(copy);
            using var cmd = c.CreateCommand();
            cmd.CommandText = "select url, title, visit_count, last_visit_time from urls where visit_count > 0 order by last_visit_time desc limit $n";
            cmd.Parameters.AddWithValue("$n", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var url = r.GetString(0);
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;
                rows.Add(new ImportedVisit(url, r.IsDBNull(1) ? "" : r.GetString(1), r.GetInt32(2), FromChromeTime(r.GetInt64(3))));
            }
        });
        return rows;
    }

    // MARK: - passwords

    /// <summary>The key Local State holds, still wrapped by Windows (it starts "DPAPI"); null if there is none.</summary>
    public static byte[]? WrappedKey(string localStateJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(localStateJson);
            var b64 = doc.RootElement.GetProperty("os_crypt").GetProperty("encrypted_key").GetString();
            if (b64 is null) return null;
            var raw = Convert.FromBase64String(b64);
            return raw.Length > 5 && raw[0] == 'D' && raw[1] == 'P' && raw[2] == 'A' && raw[3] == 'P' && raw[4] == 'I' ? raw[5..] : null;
        }
        catch { return null; }
    }

    /// <summary>A value sealed as "v10" or "v11" + 12-byte nonce + ciphertext + 16-byte tag. Anything else (the app-bound "v20", an old DPAPI-only blob) is not read here.</summary>
    public static string? Decrypt(byte[] blob, byte[] key)
    {
        if (blob.Length < 3 + 12 + 16 || blob[0] != 'v' || blob[1] != '1' || blob[2] is not ((byte)'0' or (byte)'1')) return null;
        try
        {
            var nonce = blob[3..15];
            var tag = blob[^16..];
            var cipher = blob[15..^16];
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            return System.Text.Encoding.UTF8.GetString(plain);
        }
        catch { return null; }
    }

    public static (List<ImportedLogin> Logins, int Unreadable) Logins(string dbFile, byte[] key)
    {
        var logins = new List<ImportedLogin>();
        var bad = 0;
        WithCopy(dbFile, copy =>
        {
            using var c = Open(copy);
            using var cmd = c.CreateCommand();
            cmd.CommandText = "select origin_url, username_value, password_value from logins where blacklisted_by_user = 0";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var url = r.GetString(0);
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;
                var blob = (byte[])r["password_value"];
                if (blob.Length == 0) continue;
                if (Decrypt(blob, key) is { Length: > 0 } pw) logins.Add(new ImportedLogin(url, r.IsDBNull(1) ? "" : r.GetString(1), pw)); else bad++;
            }
        });
        return (logins, bad);
    }

    // MARK: - files

    /// <summary>The browser keeps its databases open: work from a copy, with its journal.</summary>
    internal static void WithCopy(string file, Action<string> work)
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "search-import-" + Guid.NewGuid().ToString("N")[..8])).FullName;
        try
        {
            var copy = Path.Combine(dir, "db");
            using (var src = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var dst = File.Create(copy)) src.CopyTo(dst);
            foreach (var ext in new[] { "-wal", "-journal" })
                if (File.Exists(file + ext)) { try { File.Copy(file + ext, copy + ext, true); } catch { } }
            work(copy);
        }
        finally { try { SqliteConnection.ClearAllPools(); Directory.Delete(dir, true); } catch { } }
    }

    internal static SqliteConnection Open(string file)
    {
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString());
        c.Open();
        return c;
    }
}
