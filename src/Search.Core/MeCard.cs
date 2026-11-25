using System.Text.RegularExpressions;

namespace Search.Core;

public sealed class MeAddress
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Postal { get; set; } = "";
    /// <summary>A name ("France") or a two-letter code ("FR").</summary>
    public string Country { get; set; } = "";
}

/// <summary>Your own details, written once in Settings. Windows has no card of yours to read as the Mac's Contacts has, so this is it.</summary>
public sealed class MeCardData
{
    public string Given { get; set; } = "";
    public string Middle { get; set; } = "";
    public string Family { get; set; } = "";
    public string Organization { get; set; } = "";
    public List<string> Emails { get; set; } = [];
    public List<string> Phones { get; set; } = [];
    public List<MeAddress> Addresses { get; set; } = [];

    public bool IsEmpty => Given.Length + Family.Length + Middle.Length + Organization.Length == 0 && Emails.Count == 0 && Phones.Count == 0 && Addresses.Count == 0;
}

/// <summary>One row under a box, and everything for the form it is in, by autocomplete name (MeCard.swift's Offer).</summary>
public sealed record MeOffer(string Id, string Title, string Detail, Dictionary<string, string> Fields);

public static class MeCard
{
    public const string File = "mecard.json";
    public const int MostRows = 4;

    public static MeCardData Load() => Store.Read<MeCardData>(File) ?? new();
    public static void Save(MeCardData card) => Store.Write(File, Clean(card));

    static MeCardData Clean(MeCardData c)
    {
        string T(string? s) => (s ?? "").Trim();
        return new MeCardData
        {
            Given = T(c.Given), Middle = T(c.Middle), Family = T(c.Family), Organization = T(c.Organization),
            Emails = (c.Emails ?? []).Select(T).Where(s => s.Length > 0).Distinct().Take(8).ToList(),
            Phones = (c.Phones ?? []).Select(T).Where(s => s.Length > 0).Distinct().Take(8).ToList(),
            Addresses = (c.Addresses ?? []).Select(a => new MeAddress { Street = T(a.Street), City = T(a.City), State = T(a.State), Postal = T(a.Postal), Country = T(a.Country) })
                .Where(a => a.Street.Length + a.City.Length + a.State.Length + a.Postal.Length + a.Country.Length > 0).Take(8).ToList(),
        };
    }

    /// <summary>The autocomplete names the card can answer.</summary>
    public static readonly HashSet<string> Kinds =
    [
        "name", "given-name", "additional-name", "family-name", "organization",
        "email", "tel", "tel-national",
        "street-address", "address-line1", "address-line2", "address-level1", "address-level2", "postal-code", "country", "country-name",
    ];

    static bool IsAddress(string kind) => kind is "street-address" or "address-line1" or "address-line2" or "address-level1" or "address-level2" or "postal-code" or "country" or "country-name";

    /// <summary>"+1 555 0100" is "555 0100": the number without its country's code, for a box that asks for it so.</summary>
    public static string National(string phone) => Regex.Replace(phone, @"^\s*\+\d{1,3}[\s.\-]+", "");

    /// <summary>The rows under a box of this kind: one per email under an email's box, per number under a phone's, per address under an address's; under a name's, one per address. Four at most.</summary>
    public static List<MeOffer> Offers(MeCardData card, string kind)
    {
        var name = string.Join(" ", new[] { card.Given, card.Middle, card.Family }.Where(s => s.Length > 0));
        Dictionary<string, string> Fields(string? email, string? phone, MeAddress? place)
        {
            var o = new Dictionary<string, string>
            {
                ["name"] = name, ["given-name"] = card.Given, ["additional-name"] = card.Middle, ["family-name"] = card.Family,
                ["organization"] = card.Organization, ["email"] = email ?? "", ["tel"] = phone ?? "", ["tel-national"] = phone is null ? "" : National(phone),
            };
            if (place is not null)
            {
                var lines = place.Street.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
                o["street-address"] = string.Join("\n", lines);
                o["address-line1"] = lines.FirstOrDefault() ?? "";
                o["address-line2"] = string.Join(", ", lines.Skip(1));
                o["address-level2"] = place.City;
                o["address-level1"] = place.State;
                o["postal-code"] = place.Postal;
                var code = place.Country.Length == 2 ? place.Country.ToUpperInvariant() : "";
                o["country"] = code.Length > 0 ? code : place.Country;
                o["country-name"] = place.Country;
            }
            return o.Where(kv => kv.Value.Length > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        string OneLine(MeAddress a) => string.Join(", ", new[] { a.Street.Split('\n')[0].Trim(), a.City }.Where(s => s.Length > 0));
        MeOffer? Make(string id, Dictionary<string, string> all, params string?[] detail)
        {
            var shown = kind == "country" ? all.GetValueOrDefault("country-name") : kind == "street-address" ? all.GetValueOrDefault("street-address")?.Replace("\n", ", ") : all.GetValueOrDefault(kind);
            if (string.IsNullOrEmpty(shown)) return null;
            var rest = detail.Where(d => !string.IsNullOrEmpty(d) && d != shown).Distinct();
            return new MeOffer(id, shown, string.Join(" · ", rest), all);
        }

        var rows = new List<MeOffer?>();
        if (kind == "email") rows.AddRange(card.Emails.Select((e, i) => Make("email" + i, Fields(e, card.Phones.FirstOrDefault(), card.Addresses.FirstOrDefault()), name)));
        else if (kind is "tel" or "tel-national") rows.AddRange(card.Phones.Select((p, i) => Make("tel" + i, Fields(card.Emails.FirstOrDefault(), p, card.Addresses.FirstOrDefault()), name)));
        else if (IsAddress(kind) || kind is "name" or "given-name" or "additional-name" or "family-name" or "organization")
        {
            if (card.Addresses.Count == 0) rows.Add(Make("card", Fields(card.Emails.FirstOrDefault(), card.Phones.FirstOrDefault(), null), card.Emails.FirstOrDefault()));
            else rows.AddRange(card.Addresses.Select((a, i) => Make("place" + i, Fields(card.Emails.FirstOrDefault(), card.Phones.FirstOrDefault(), a), IsAddress(kind) ? name : OneLine(a))));
        }
        return rows.OfType<MeOffer>().DistinctBy(o => o.Title + "|" + o.Detail).Take(MostRows).ToList();
    }
}
