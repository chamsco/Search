using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Search.Core.Import;

/// <summary>Firefox and Zen: a profile folder. Bookmarks and history are in places.sqlite. Saved sign-ins are in logins.json, sealed with a key in key4.db that NSS keeps (PBES2 with AES, or the older 3DES scheme), open to anyone who can read the folder unless a primary password is set; with one set they are left alone.</summary>
public static class FirefoxImport
{
    public static List<ImportedBookmark> Bookmarks(string placesDb)
    {
        var found = new List<ImportedBookmark>();
        ChromiumImport.WithCopy(placesDb, copy =>
        {
            using var c = ChromiumImport.Open(copy);
            using var cmd = c.CreateCommand();
            cmd.CommandText = "select b.title, p.url, f.title, f.parent from moz_bookmarks b join moz_places p on p.id = b.fk left join moz_bookmarks f on f.id = b.parent where b.type = 1 and p.url like 'http%' order by b.position";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                // Firefox's own roots (menu, toolbar, unfiled) hold bookmarks at the top: their parent is the root row, id 1.
                var parentRoot = !r.IsDBNull(3) && r.GetInt64(3) == 1;
                var folder = r.IsDBNull(2) || parentRoot ? null : r.GetString(2);
                if (folder is "menu" or "toolbar" or "unfiled" or "mobile") folder = null;
                found.Add(new ImportedBookmark(r.IsDBNull(0) ? "" : r.GetString(0), r.GetString(1), folder));
            }
        });
        return found;
    }

    public static List<ImportedVisit> History(string placesDb, int limit = 20000)
    {
        var rows = new List<ImportedVisit>();
        ChromiumImport.WithCopy(placesDb, copy =>
        {
            using var c = ChromiumImport.Open(copy);
            using var cmd = c.CreateCommand();
            cmd.CommandText = "select url, title, visit_count, last_visit_date from moz_places where visit_count > 0 and url like 'http%' and last_visit_date is not null order by last_visit_date desc limit $n";
            cmd.Parameters.AddWithValue("$n", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                rows.Add(new ImportedVisit(r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1), r.GetInt32(2), DateTime.UnixEpoch.AddTicks(r.GetInt64(3) * 10)));
        });
        return rows;
    }

    // MARK: - saved sign-ins

    public enum LoginsResult { Read, NoStore, NeedsPrimaryPassword, Unreadable }

    public static (LoginsResult Result, List<ImportedLogin> Logins) Logins(string profileDir)
    {
        var loginsFile = Path.Combine(profileDir, "logins.json");
        var keyFile = Path.Combine(profileDir, "key4.db");
        if (!File.Exists(loginsFile) || !File.Exists(keyFile)) return (LoginsResult.NoStore, []);
        byte[]? key = null;
        try
        {
            ChromiumImport.WithCopy(keyFile, copy =>
            {
                using var c = ChromiumImport.Open(copy);
                key = Key(c, "");
            });
        }
        catch { return (LoginsResult.Unreadable, []); }
        if (key is null) return (LoginsResult.NeedsPrimaryPassword, []);
        var logins = new List<ImportedLogin>();
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(loginsFile));
            foreach (var l in doc.RootElement.GetProperty("logins").EnumerateArray())
            {
                var url = l.TryGetProperty("hostname", out var h) ? h.GetString() ?? "" : "";
                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;
                var user = OpenField(l.GetProperty("encryptedUsername").GetString() ?? "", key) ?? "";
                var pw = OpenField(l.GetProperty("encryptedPassword").GetString() ?? "", key);
                if (!string.IsNullOrEmpty(pw)) logins.Add(new ImportedLogin(url, user, pw));
            }
        }
        catch { return (LoginsResult.Unreadable, logins); }
        return (LoginsResult.Read, logins);
    }

    /// <summary>The key NSS keeps, opened with the primary password (empty when none is set); null if that password is not it.</summary>
    internal static byte[]? Key(SqliteConnection db, string primary)
    {
        byte[] globalSalt, check;
        using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = "select item1, item2 from metadata where id = 'password'";
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;
            globalSalt = (byte[])r["item1"]; check = (byte[])r["item2"];
        }
        var proof = Decrypt(check, globalSalt, primary);
        if (proof is null || !Encoding.ASCII.GetString(proof).StartsWith("password-check", StringComparison.Ordinal)) return null;
        using var keys = db.CreateCommand();
        keys.CommandText = "select a11, a102 from nssPrivate";
        using var rows = keys.ExecuteReader();
        while (rows.Read())
        {
            if (rows.IsDBNull(0)) continue;
            var opened = Decrypt((byte[])rows["a11"], globalSalt, primary);
            if (opened is { Length: >= 24 }) return opened;
        }
        return null;
    }

    const string Pbes2 = "1.2.840.113549.1.5.13", Pbkdf2 = "1.2.840.113549.1.5.12", Aes256Cbc = "2.16.840.1.101.3.4.1.42", TripleDes = "1.2.840.113549.3.7", PbeSha1TripleDes = "1.2.840.113549.1.12.5.1.3";

    /// <summary>NSS's sealed blobs: SEQUENCE { SEQUENCE { algorithm, parameters }, OCTET STRING ciphertext }.</summary>
    internal static byte[]? Decrypt(byte[] der, byte[] globalSalt, string primary)
    {
        try
        {
            var outer = new AsnReader(der, AsnEncodingRules.DER).ReadSequence();
            var algo = outer.ReadSequence();
            var oid = algo.ReadObjectIdentifier();
            var cipher = outer.ReadOctetString();
            var pw = SHA1.HashData([.. globalSalt, .. Encoding.UTF8.GetBytes(primary)]);
            if (oid == Pbes2)
            {
                var pr = algo.ReadSequence();
                var kdf = pr.ReadSequence();
                if (kdf.ReadObjectIdentifier() != Pbkdf2) return null;
                var kp = kdf.ReadSequence();
                var salt = kp.ReadOctetString();
                var iterations = (int)kp.ReadInteger();
                var length = 32;
                if (kp.HasData && kp.PeekTag().TagValue == (int)UniversalTagNumber.Integer) length = (int)kp.ReadInteger();
                var enc = pr.ReadSequence();
                var encOid = enc.ReadObjectIdentifier();
                var ivRaw = enc.ReadOctetString();
                if (encOid != Aes256Cbc) return null;
                var iv = ivRaw.Length == 14 ? [0x04, 0x0e, .. ivRaw] : ivRaw;
                var key = Rfc2898DeriveBytes.Pbkdf2(pw, salt, iterations, HashAlgorithmName.SHA256, length);
                using var aes = Aes.Create(); aes.Key = key; aes.IV = iv;
                return aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7);
            }
            if (oid == PbeSha1TripleDes)
            {
                var pr = algo.ReadSequence();
                var entrySalt = pr.ReadOctetString();
                var hp = SHA1.HashData([.. globalSalt, .. Encoding.UTF8.GetBytes(primary)]);
                var pes = new byte[20]; entrySalt.CopyTo(pes, 0);
                var chp = SHA1.HashData([.. hp, .. entrySalt]);
                var k1 = HMACSHA1.HashData(chp, pes.Concat(entrySalt).ToArray());
                var tk = HMACSHA1.HashData(chp, pes);
                var k2 = HMACSHA1.HashData(chp, tk.Concat(entrySalt).ToArray());
                var k = k1.Concat(k2).ToArray();
                using var des = TripleDES.Create(); des.Key = k[..24];
                return des.DecryptCbc(cipher, k[^8..], PaddingMode.PKCS7);
            }
        }
        catch { }
        return null;
    }

    /// <summary>One field of logins.json: base64 of SEQUENCE { key id, SEQUENCE { cipher, iv }, ciphertext }, under the key NSS kept.</summary>
    internal static string? OpenField(string base64, byte[] key)
    {
        try
        {
            var seq = new AsnReader(Convert.FromBase64String(base64), AsnEncodingRules.DER).ReadSequence();
            seq.ReadOctetString();
            var algo = seq.ReadSequence();
            var oid = algo.ReadObjectIdentifier();
            var iv = algo.ReadOctetString();
            var cipher = seq.ReadOctetString();
            byte[] plain;
            if (oid == TripleDes) { using var des = TripleDES.Create(); des.Key = key[..24]; plain = des.DecryptCbc(cipher, iv, PaddingMode.PKCS7); }
            else if (oid == Aes256Cbc) { using var aes = Aes.Create(); aes.Key = key.Length >= 32 ? key[..32] : key; plain = aes.DecryptCbc(cipher, iv, PaddingMode.PKCS7); }
            else return null;
            return Encoding.UTF8.GetString(plain);
        }
        catch { return null; }
    }
}
