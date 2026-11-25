using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Search.Core.Import;
using Xunit;

namespace Search.Core.Tests;

public class ImportFirefoxTests
{
    static string Dir() => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ff-" + Guid.NewGuid().ToString("N")[..8])).FullName;

    static void Exec(string db, Action<SqliteConnection> work)
    {
        using var c = new SqliteConnection($"Data Source={db};Pooling=false"); c.Open(); work(c);
    }

    static void Sql(SqliteConnection c, string sql, params (string, object)[] args)
    {
        using var cmd = c.CreateCommand(); cmd.CommandText = sql;
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        cmd.ExecuteNonQuery();
    }

    /// <summary>NSS's PBES2 seal, built to the spec Firefox writes, so the reader is checked against the format and not against itself.</summary>
    static byte[] SealPbes2(byte[] plain, byte[] globalSalt, string primary)
    {
        var pw = SHA1.HashData([.. globalSalt, .. Encoding.UTF8.GetBytes(primary)]);
        var salt = RandomNumberGenerator.GetBytes(32); var iv14 = RandomNumberGenerator.GetBytes(14);
        var key = Rfc2898DeriveBytes.Pbkdf2(pw, salt, 10000, HashAlgorithmName.SHA256, 32);
        using var aes = Aes.Create();
        aes.Key = key;
        byte[] iv = new byte[] { 0x04, 0x0e }.Concat(iv14).ToArray();
        var cipher = aes.EncryptCbc(plain, iv, PaddingMode.PKCS7);
        var w = new AsnWriter(AsnEncodingRules.DER);
        using (w.PushSequence())
        {
            using (w.PushSequence())
            {
                w.WriteObjectIdentifier("1.2.840.113549.1.5.13");
                using (w.PushSequence())
                {
                    using (w.PushSequence())
                    {
                        w.WriteObjectIdentifier("1.2.840.113549.1.5.12");
                        using (w.PushSequence())
                        {
                            w.WriteOctetString(salt); w.WriteInteger(10000); w.WriteInteger(32);
                            using (w.PushSequence()) { w.WriteObjectIdentifier("1.2.840.113549.2.9"); w.WriteNull(); }
                        }
                    }
                    using (w.PushSequence()) { w.WriteObjectIdentifier("2.16.840.1.101.3.4.1.42"); w.WriteOctetString(iv14); }
                }
            }
            w.WriteOctetString(cipher);
        }
        return w.Encode();
    }

    static string SealField(string text, byte[] key24)
    {
        var iv = RandomNumberGenerator.GetBytes(8);
        using var des = TripleDES.Create(); des.Key = key24;
        byte[] plain = Encoding.UTF8.GetBytes(text);
        var cipher = des.EncryptCbc(plain, iv, PaddingMode.PKCS7);
        var w = new AsnWriter(AsnEncodingRules.DER);
        using (w.PushSequence())
        {
            w.WriteOctetString(new byte[16]);
            using (w.PushSequence()) { w.WriteObjectIdentifier("1.2.840.113549.3.7"); w.WriteOctetString(iv); }
            w.WriteOctetString(cipher);
        }
        return Convert.ToBase64String(w.Encode());
    }

    static string Profile(string primary, byte[] key24, params (string Host, string User, string Pw)[] logins)
    {
        var dir = Dir();
        var salt = RandomNumberGenerator.GetBytes(20);
        Exec(Path.Combine(dir, "key4.db"), c =>
        {
            Sql(c, "create table metadata (id text, item1 blob, item2 blob)");
            Sql(c, "create table nssPrivate (a11 blob, a102 blob)");
            Sql(c, "insert into metadata values ('password', $s, $c)", ("$s", salt), ("$c", SealPbes2(Encoding.ASCII.GetBytes("password-check\x02\x02"), salt, primary)));
            Sql(c, "insert into nssPrivate values ($k, $i)", ("$k", SealPbes2(key24, salt, primary)), ("$i", new byte[] { 1 }));
        });
        var items = string.Join(",", logins.Select(l => $"{{\"hostname\":\"{l.Host}\",\"encryptedUsername\":\"{SealField(l.User, key24)}\",\"encryptedPassword\":\"{SealField(l.Pw, key24)}\"}}"));
        File.WriteAllText(Path.Combine(dir, "logins.json"), "{\"logins\":[" + items + "]}");
        return dir;
    }

    [Fact]
    public void SavedSignInsAreOpenedWithNoPrimaryPassword()
    {
        var dir = Profile("", RandomNumberGenerator.GetBytes(24), ("https://site.example", "ana", "s3cret!"), ("ftp://skip.example", "x", "y"));
        var (result, logins) = FirefoxImport.Logins(dir);
        Assert.Equal(FirefoxImport.LoginsResult.Read, result);
        var one = Assert.Single(logins);
        Assert.Equal(("https://site.example", "ana", "s3cret!"), (one.Url, one.User, one.Password));
    }

    [Fact]
    public void APrimaryPasswordLeavesThemAlone()
    {
        var dir = Profile("hunter2", RandomNumberGenerator.GetBytes(24), ("https://site.example", "ana", "s3cret!"));
        var (result, logins) = FirefoxImport.Logins(dir);
        Assert.Equal(FirefoxImport.LoginsResult.NeedsPrimaryPassword, result);
        Assert.Empty(logins);
    }

    [Fact]
    public void NoStoreIsSaidSo() => Assert.Equal(FirefoxImport.LoginsResult.NoStore, FirefoxImport.Logins(Dir()).Result);

    [Fact]
    public void PlacesGiveBookmarksWithFoldersAndHistory()
    {
        var db = Path.Combine(Dir(), "places.sqlite");
        Exec(db, c =>
        {
            Sql(c, "create table moz_places (id integer primary key, url text, title text, visit_count integer, last_visit_date integer)");
            Sql(c, "create table moz_bookmarks (id integer primary key, type integer, fk integer, parent integer, position integer, title text)");
            Sql(c, "insert into moz_places values (1, 'https://a.example/', 'A', 4, 1740830400000000), (2, 'https://b.example/', 'B', 1, 1740830400000000), (3, 'about:blank', 'x', 5, 1740830400000000)");
            Sql(c, "insert into moz_bookmarks values (1, 2, null, 0, 0, ''), (2, 2, null, 1, 0, 'toolbar'), (3, 2, null, 2, 0, 'Reading'), (4, 1, 1, 2, 0, 'A bookmark'), (5, 1, 2, 3, 0, 'In a folder')");
        });
        var b = FirefoxImport.Bookmarks(db);
        Assert.Contains(b, x => x.Title == "A bookmark" && x.Folder is null);
        Assert.Contains(b, x => x.Title == "In a folder" && x.Folder == "Reading");
        var h = FirefoxImport.History(db);
        Assert.Equal(2, h.Count);
        Assert.Equal(new DateTime(2025, 3, 1, 12, 0, 0, DateTimeKind.Utc), h.First(x => x.Title == "A").Last);
    }
}
