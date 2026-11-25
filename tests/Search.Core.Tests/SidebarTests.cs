using Search.Core.Import;
using Xunit;

namespace Search.Core.Tests;

public class SidebarTests
{
    const string ArcJson = """
    {"sidebar":{"containers":[{"global":{}},{"spaces":[
      {"title":"Work","newContainerIDs":["unpinned","u1",{"pinned":{}},"p1"],"customInfo":{"iconType":{"emoji_v2":"💼"}},"profile":{"default":{}}},
      {"title":"Home","newContainerIDs":["pinned","p2"],"profile":{"default":{}}}],
     "topAppsContainerIDs":[{"default":true},"top1"],
     "items":[
      {"id":"p1","childrenIds":["t1","f1"],"data":{"itemContainer":{}}},
      {"id":"t1","title":"Docs","data":{"tab":{"savedURL":"https://docs.example.com/","savedTitle":"x"}}},
      {"id":"f1","title":"Reading","childrenIds":["t2"],"data":{"list":{}}},
      {"id":"t2","title":null,"data":{"tab":{"savedURL":"https://news.example.org/a","savedTitle":"News"}}},
      {"id":"p2","childrenIds":["t3"],"data":{"itemContainer":{}}},
      {"id":"t3","title":"Mail","data":{"tab":{"savedURL":"javascript:alert(1)"}}},
      {"id":"top1","childrenIds":["t4"],"data":{"itemContainer":{}}},
      {"id":"t4","title":"Chat","data":{"tab":{"savedURL":"https://chat.example.com"}}}]}]}}
    """;

    [Fact]
    public void ArcSpacesPinsFoldersAndFavourites()
    {
        var s = ArcSidebar.Parse(ArcJson)!;
        Assert.Equal(2, s.Spaces.Count);
        var work = s.Spaces[0];
        Assert.Equal("Work", work.Name);
        Assert.Equal(2, work.Pinned.Count);
        Assert.Equal("Docs", work.Pinned[0].Item!.Title);
        Assert.True(work.Pinned[1].IsFolder);
        Assert.Equal("Reading", work.Pinned[1].FolderTitle);
        Assert.Equal("News", work.Pinned[1].Children[0].Item!.Title);
        Assert.Empty(s.Spaces[1].Pinned); // javascript: is not a page
        Assert.Single(s.Favorites);
        Assert.Equal("https://chat.example.com/", s.Favorites[0].Url);
    }

    [Fact]
    public void ArcRejectsNonsense()
    {
        Assert.Null(ArcSidebar.Parse("not json"));
        Assert.Null(ArcSidebar.Parse("{}"));
    }

    static byte[] MozLz4Literal(byte[] data)
    {
        // One LZ4 sequence holding only literals, behind the mozLz40 header.
        var block = new List<byte>();
        var n = data.Length;
        if (n < 15) block.Add((byte)(n << 4));
        else { block.Add(0xF0); var r = n - 15; while (r >= 255) { block.Add(255); r -= 255; } block.Add((byte)r); }
        block.AddRange(data);
        var all = new List<byte>("mozLz40\0"u8.ToArray());
        all.AddRange(BitConverter.GetBytes(n));
        all.AddRange(block);
        return all.ToArray();
    }

    [Fact]
    public void MozLz4LiteralBlock()
    {
        var text = System.Text.Encoding.UTF8.GetBytes("{\"spaces\":[{\"name\":\"Work\"}],\"padding\":\"" + new string('x', 300) + "\"}");
        Assert.Equal(text, MozLz4.Decode(MozLz4Literal(text)));
    }

    [Fact]
    public void MozLz4MatchBlock()
    {
        // "abcd" then a match of length 8 at offset 4 → "abcdabcdabcd"
        var block = new byte[] { 0x44, (byte)'a', (byte)'b', (byte)'c', (byte)'d', 4, 0 };
        var packed = new byte[12 + block.Length];
        "mozLz40\0"u8.CopyTo(packed);
        BitConverter.GetBytes(12).CopyTo(packed, 8);
        block.CopyTo(packed, 12);
        // the last sequence must be literal-only; add one empty literal token
        packed = packed.Concat(new byte[] { 0x00 }).ToArray();
        Assert.Equal("abcdabcdabcd", System.Text.Encoding.ASCII.GetString(MozLz4.Decode(packed)!));
    }

    [Fact]
    public void MozLz4RefusesDamage()
    {
        Assert.Null(MozLz4.Decode([1, 2, 3]));
        var bad = MozLz4Literal([1, 2, 3, 4]);
        bad[8] = 99; // claims more bytes than there are
        Assert.Null(MozLz4.Decode(bad));
    }
}
