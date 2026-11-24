using Search.Core;
using Xunit;

namespace Search.Core.Tests;

public class GroupTests
{
    static readonly Guid A = Guid.NewGuid(), B = Guid.NewGuid();

    [Fact]
    public void ARunStaysAndALaterOneIsReleased()
    {
        var r = Groups.Normalize([A, A, null, A, B], [A, B]);
        Assert.Equal(new Guid?[] { A, A, null, null, B }, r);
    }

    [Fact]
    public void AnUnknownGroupIsNone() =>
        Assert.Equal(new Guid?[] { null, A }, Groups.Normalize([Guid.NewGuid(), A], [A]));

    [Fact]
    public void DroppedInsideAGroupJoinsIt() => Assert.Equal(A, Groups.Adopt(A, A, null));

    [Fact]
    public void DroppedBesideItsOwnGroupStays() => Assert.Equal(A, Groups.Adopt(A, null, A));

    [Fact]
    public void DroppedBetweenTwoGroupsOrAwayIsNone()
    {
        Assert.Null(Groups.Adopt(A, B, null));
        Assert.Null(Groups.Adopt(B, null, A));
    }

    [Fact]
    public void EmptyGroupsAreDropped()
    {
        var kept = Groups.Kept([new TabGroup { Id = A }, new TabGroup { Id = B }], [B, null]);
        Assert.Equal([B], kept.Select(g => g.Id));
    }
}

public class PinTreeTests
{
    static PinFolder F(string n, Guid? parent = null, bool collapsed = false) => new() { Name = n, Parent = parent, Collapsed = collapsed };

    [Fact]
    public void FoldersComeFirstThenPins()
    {
        var work = F("Work");
        var pin1 = Guid.NewGuid(); var pin2 = Guid.NewGuid(); var inWork = Guid.NewGuid();
        var lines = PinTree.Lines([work], [(pin1, null), (inWork, work.Id), (pin2, null)]);
        Assert.Equal(new string?[] { "Work", null, null, null }, lines.Select(l => l.Folder?.Name).ToArray());
        Assert.Equal([inWork, pin1, pin2], lines.Where(l => l.Pin is not null).Select(l => l.Pin!.Value).ToArray());
        Assert.Equal([0, 1, 0, 0], lines.Select(l => l.Depth).ToArray());
    }

    [Fact]
    public void AFoldedFolderShowsItsHeadingAlone()
    {
        var work = F("Work", collapsed: true);
        var inner = F("Inner", work.Id);
        var lines = PinTree.Lines([work, inner], [(Guid.NewGuid(), work.Id)]);
        Assert.Single(lines);
    }

    [Fact]
    public void NestingStopsAtThreeLevels()
    {
        var a = F("a"); var b = F("b", a.Id); var c = F("c", b.Id); var d = F("d", c.Id);
        var clean = PinTree.Sanitized([a, b, c, d]);
        Assert.Equal(3, PinTree.Depth(clean.Single(x => x.Name == "c").Id, clean));
        Assert.Equal(b.Id, clean.Single(x => x.Name == "d").Parent);
    }

    [Fact]
    public void AGoneParentOrACycleGoesToTheTop()
    {
        var a = F("a", Guid.NewGuid());
        var b = F("b"); var c = F("c", b.Id); b.Parent = c.Id;
        var clean = PinTree.Sanitized([a, b, c]);
        Assert.Null(clean[0].Parent);
        Assert.Null(clean[1].Parent);
    }

    [Fact]
    public void AFolderMayNotGoIntoItselfOrDeeperThanThree()
    {
        var a = F("a"); var b = F("b", a.Id); var c = F("c", b.Id); var other = F("other"); var inner = F("inner", other.Id);
        var all = new List<PinFolder> { a, b, c, other, inner };
        Assert.False(PinTree.CanHold(a.Id, a.Id, all));
        Assert.False(PinTree.CanHold(b.Id, a.Id, all));
        Assert.False(PinTree.CanHold(c.Id, other.Id, all)); // other holds inner: 3 + 2 levels
        Assert.True(PinTree.CanHold(null, c.Id, all));
        Assert.True(PinTree.CanHold(a.Id, other.Id, all)); // 1 + 2 levels = 3
    }
}
