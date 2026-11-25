using System.Text.Json;
using Xunit;

namespace Search.Core.Tests;

public class SyncSeedTests
{
    // A file written by hand, as the CI step writes it: another PC's, in the folder before this one has ever joined.
    [Fact]
    public void JoiningAFolderTakesWhatIsThere()
    {
        var shared = Path.Combine(Path.GetTempPath(), "seed-" + Guid.NewGuid().ToString("N")[..6]);
        var data = Path.Combine(Path.GetTempPath(), "seedd-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(data);
        var was = Store.Root; Store.Root = data;
        try
        {
            string E(string v) => "{\"v\":" + JsonSerializer.Serialize(v) + ",\"t\":100,\"d\":\"otherpc\"}";
            string file(string scope, string items) => "{\"format\":1,\"device\":\"otherpc\",\"name\":\"Other PC\",\"scope\":\"" + scope + "\",\"written\":100,\"items\":{" + items + "}}";
            var personal = "profile-000000000000000000000000000000a1";
            Directory.CreateDirectory(Path.Combine(shared, "Search Sync", personal)); Directory.CreateDirectory(Path.Combine(shared, "Search Sync", "app"));
            var space = "5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a5a"; var pin = "6b6b6b6b6b6b6b6b6b6b6b6b6b6b6b6b"; var mark = "7c7c7c7c7c7c7c7c7c7c7c7c7c7c7c7c";
            File.WriteAllText(Path.Combine(shared, "Search Sync", personal, "otherpc.json"), file(personal,
                "\"bm:" + mark + "\":" + E("{\"Title\":\"Synced Mark\",\"Url\":\"https://synced.example/\",\"Folder\":null}") + "," +
                "\"space:" + space + "\":" + E("{\"Name\":\"Synced Space\",\"Colour\":2,\"Colour2\":0,\"Icon\":null,\"Shares\":true,\"Strength\":0}") + "," +
                "\"pin:" + space + ":" + pin + "\":" + E("{\"Url\":\"https://synced-pin.example/\",\"Title\":\"Synced Pin\",\"Name\":null,\"Folder\":null,\"Muted\":false}") + "," +
                "\"pinorder:" + space + "\":" + E("[\"" + pin + "\"]")));
            File.WriteAllText(Path.Combine(shared, "Search Sync", "app", "otherpc.json"), file("app", "\"set:search.suggest\":" + E("false")));
            File.WriteAllText(Path.Combine(data, "sync.json"), JsonSerializer.Serialize(new { on = true, folder = shared }));

            var prefs = Prefs.Load(); var book = SpaceBook.Load(); var marks = Bookmarks.Load();
            var sync = new FolderSync(prefs, book, _ => marks);
            var r = sync.Run(5000);
            Assert.Null(r.Problem);
            Assert.Equal(1, r.Devices);
            Assert.False(prefs.Data.EngineSuggests);
            Assert.Equal("Synced Mark", marks.Items.Single().Title);
            Assert.Contains(book.Spaces, s => s.Name == "Synced Space");
            Assert.Contains("synced-pin.example", File.ReadAllText(Path.Combine(data, "session-" + space + ".json")));
        }
        finally { Store.Root = was; try { Directory.Delete(shared, true); Directory.Delete(data, true); } catch { } }
    }
}
