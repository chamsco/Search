using Xunit;

namespace Search.Core.Tests;

public class MeCardTests
{
    static MeCardData Ana() => new()
    {
        Given = "Ana", Family = "Diaz", Organization = "Acme",
        Emails = ["ana@example.com", "ana@work.example"], Phones = ["+33 6 12 34 56 78"],
        Addresses = [new MeAddress { Street = "1 Rue Haute\nApt 4", City = "Lyon", State = "ARA", Postal = "69001", Country = "FR" },
                     new MeAddress { Street = "9 Low St", City = "Leeds", Postal = "LS1", Country = "United Kingdom" }],
    };

    [Fact]
    public void EmailBoxGetsOneRowPerEmailWithTheFormBesideIt()
    {
        var rows = MeCard.Offers(Ana(), "email");
        Assert.Equal(["ana@example.com", "ana@work.example"], rows.Select(r => r.Title));
        Assert.Equal("Ana Diaz", rows[0].Fields["name"]);
        Assert.Equal("69001", rows[0].Fields["postal-code"]);
        Assert.Equal("ana@work.example", rows[1].Fields["email"]);
    }

    [Fact]
    public void PhoneBoxAndNationalForm()
    {
        var row = MeCard.Offers(Ana(), "tel-national").Single();
        Assert.Equal("6 12 34 56 78", row.Title);
        Assert.Equal("+33 6 12 34 56 78", row.Fields["tel"]);
    }

    [Fact]
    public void AddressBoxesGetOneRowPerAddress()
    {
        var rows = MeCard.Offers(Ana(), "street-address");
        Assert.Equal(["1 Rue Haute, Apt 4", "9 Low St"], rows.Select(r => r.Title));
        Assert.Equal("1 Rue Haute", rows[0].Fields["address-line1"]);
        Assert.Equal("Apt 4", rows[0].Fields["address-line2"]);
        Assert.Equal("FR", rows[0].Fields["country"]);
        Assert.Equal("United Kingdom", rows[1].Fields["country"]);
        Assert.Equal("France", MeCard.Offers(new MeCardData { Addresses = [new MeAddress { Country = "France" }] }, "country").Single().Title);
    }

    [Fact]
    public void NameBoxWithNoAddressStillOffersTheCard()
    {
        var card = new MeCardData { Given = "Ana", Family = "Diaz", Emails = ["a@b.c"] };
        var row = MeCard.Offers(card, "given-name").Single();
        Assert.Equal("Ana", row.Title);
        Assert.Equal("Ana Diaz", row.Fields["name"]);
    }

    [Fact]
    public void EmptyCardOffersNothingAndFourAtMost()
    {
        Assert.Empty(MeCard.Offers(new MeCardData(), "email"));
        var many = new MeCardData { Emails = Enumerable.Range(0, 7).Select(i => $"a{i}@x.y").ToList() };
        Assert.Equal(4, MeCard.Offers(many, "email").Count);
    }

    [Fact]
    public void SaveTrimsAndDropsBlanks()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mecard-" + Guid.NewGuid().ToString("N")[..6]);
        var before = Store.Root; Store.Root = dir;
        try
        {
            MeCard.Save(new MeCardData { Given = " Ana ", Emails = [" a@b.c ", "", "a@b.c"], Addresses = [new MeAddress()] });
            var back = MeCard.Load();
            Assert.Equal("Ana", back.Given); Assert.Single(back.Emails); Assert.Empty(back.Addresses);
        }
        finally { Store.Root = before; try { Directory.Delete(dir, true); } catch { } }
    }
}
