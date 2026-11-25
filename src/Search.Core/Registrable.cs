namespace Search.Core;

/// <summary>A site's registrable domain (Registrable.swift): example.com for accounts.example.com, example.co.uk for www.example.co.uk. Addresses and single names are left whole. A compact list of the multi-part suffixes people actually meet stands in for the full public suffix list.</summary>
public static class Registrable
{
    static readonly HashSet<string> Suffixes = new(StringComparer.Ordinal)
    {
        "co.uk", "org.uk", "ac.uk", "gov.uk", "me.uk", "ltd.uk", "plc.uk", "net.uk", "sch.uk",
        "com.au", "net.au", "org.au", "edu.au", "gov.au", "co.nz", "org.nz", "net.nz", "govt.nz", "ac.nz",
        "co.jp", "ne.jp", "or.jp", "ac.jp", "go.jp", "co.kr", "or.kr", "ac.kr", "go.kr",
        "com.cn", "net.cn", "org.cn", "gov.cn", "edu.cn", "com.hk", "org.hk", "com.tw", "org.tw", "com.sg", "edu.sg",
        "com.br", "net.br", "org.br", "gov.br", "com.mx", "org.mx", "com.ar", "com.co", "com.pe", "com.ve", "com.uy",
        "co.in", "net.in", "org.in", "gov.in", "ac.in", "co.id", "co.il", "co.th", "co.za", "org.za", "com.tr", "com.ua",
        "com.pl", "com.ru", "com.sa", "com.eg", "com.ng", "com.pk", "com.my", "com.ph", "com.vn", "com.bd", "com.np",
        "github.io", "gitlab.io", "herokuapp.com", "vercel.app", "netlify.app", "pages.dev", "workers.dev", "web.app", "firebaseapp.com",
        "blogspot.com", "appspot.com", "azurewebsites.net", "cloudfront.net", "s3.amazonaws.com", "myshopify.com",
    };

    public static string Domain(string host)
    {
        var name = (host ?? "").ToLowerInvariant().Trim('.');
        var labels = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var address = name.Contains(':') || labels.Length > 0 && labels.All(l => l.All(char.IsDigit));
        if (address || labels.Length < 2 || Suffixes.Contains(name)) return name;
        for (var count = labels.Length - 1; count >= 1; count--)
            if (Suffixes.Contains(string.Join('.', labels.Skip(labels.Length - count)))) return string.Join('.', labels.Skip(labels.Length - count - 1));
        return string.Join('.', labels.Skip(labels.Length - 2));
    }

    public static bool SameSite(string a, string b)
    {
        a = a.ToLowerInvariant(); b = b.ToLowerInvariant();
        if (a == b) return true;
        var left = Domain(a);
        return left.Contains('.') && left == Domain(b);
    }
}
