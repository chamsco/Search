using Search.Core;
using Xunit;

namespace Search.Core.Tests;

[Collection("NoParallel")]
public class ShortcutTests
{
    static Shortcuts Fresh()
    {
        Store.Root = Path.Combine(Path.GetTempPath(), "search-sc-" + Guid.NewGuid().ToString("N")[..8]);
        return Shortcuts.Load();
    }

    [Fact]
    public void NoTwoCommandsStartOnTheSameKey()
    {
        var dupes = Commands.All.Where(c => c.Default is not null).GroupBy(c => c.Default).Where(g => g.Count() > 1).Select(g => g.Key!.ToString()).ToList();
        Assert.Empty(dupes);
    }

    [Fact]
    public void NoCommandStartsOnAKeyThatTypesOrEditsThePage() =>
        Assert.All(Commands.All.Where(c => c.Default is not null), c => Assert.Null(Shortcuts.Refuse(c.Default!)));

    [Theory]
    [InlineData("Ctrl+Shift+T")]
    [InlineData("Ctrl+Alt+Shift+C")]
    [InlineData("F11")]
    [InlineData("Alt+Left")]
    [InlineData("Ctrl+,")]
    public void KeysReadBackAsWritten(string text) => Assert.Equal(text, KeyCombo.Parse(text)!.ToString());

    [Fact]
    public void ModifiersComeInCtrlAltShiftOrderWhateverTheInput() =>
        Assert.Equal("Ctrl+Alt+Shift+K", KeyCombo.Parse("shift+alt+ctrl+k")!.ToString());

    [Fact]
    public void ABareLetterBelongsToThePage()
    {
        Assert.NotNull(Shortcuts.Refuse(KeyCombo.Parse("K")!));
        Assert.Null(Shortcuts.Refuse(KeyCombo.Parse("F5")!));
        Assert.NotNull(Shortcuts.Refuse(KeyCombo.Parse("Ctrl+C")!));
    }

    [Fact]
    public void AKeyGivenToAnotherCommandIsTakenFromTheFirst()
    {
        var s = Fresh();
        var taken = s.Set("file.newWindow", KeyCombo.Parse("Ctrl+T"));
        Assert.Equal("file.newTab", taken!.Id);
        Assert.Equal("Ctrl+T", s.KeyFor("file.newWindow")!.ToString());
        Assert.Null(s.KeyFor("file.newTab"));
        Assert.Equal("file.newWindow", s.CommandFor(KeyCombo.Parse("Ctrl+T")!)!.Id);
    }

    [Fact]
    public void NoKeyAtAll()
    {
        var s = Fresh();
        s.Set("file.print", null);
        Assert.Null(s.KeyFor("file.print"));
        Assert.Null(s.CommandFor(KeyCombo.Parse("Ctrl+P")!));
    }

    [Fact]
    public void ResettingBringsTheOwnKeyBackAndTakesItFromWhoHasIt()
    {
        var s = Fresh();
        s.Set("file.newWindow", KeyCombo.Parse("Ctrl+T"));
        var lost = s.Reset("file.newTab");
        Assert.Equal("file.newWindow", lost!.Id);
        Assert.Equal("Ctrl+T", s.KeyFor("file.newTab")!.ToString());
        Assert.Null(s.KeyFor("file.newWindow"));
    }

    [Fact]
    public void ChangesSurviveALoadAndGoingBackToTheOwnKeyForgetsThem()
    {
        var s = Fresh();
        s.Set("file.print", KeyCombo.Parse("Ctrl+Alt+P"));
        Assert.Equal("Ctrl+Alt+P", Shortcuts.Load().KeyFor("file.print")!.ToString());
        s.Set("file.print", KeyCombo.Parse("Ctrl+P"));
        Assert.False(s.IsChanged("file.print"));
    }
}
