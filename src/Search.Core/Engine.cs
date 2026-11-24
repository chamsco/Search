namespace Search.Core;

/// <summary>Search engines. Port of Sources/Search/Engine.swift.</summary>
public enum Engine { Google, DuckDuckGo, Bing, Ecosia, Startpage, Kagi, Brave, Qwant, Custom }

public static class Engines
{
    public const Engine Standard = Engine.Google;

    public static string Title(this Engine e) => e switch
    {
        Engine.Google => "Google",
        Engine.DuckDuckGo => "DuckDuckGo",
        Engine.Bing => "Bing",
        Engine.Ecosia => "Ecosia",
        Engine.Startpage => "Startpage",
        Engine.Kagi => "Kagi",
        Engine.Brave => "Brave Search",
        Engine.Qwant => "Qwant",
        _ => "Custom",
    };

    public static string Template(this Engine e, string custom = "") => e switch
    {
        Engine.Google => "https://www.google.com/search?q=%s",
        Engine.DuckDuckGo => "https://duckduckgo.com/?q=%s",
        Engine.Bing => "https://www.bing.com/search?q=%s",
        Engine.Ecosia => "https://www.ecosia.org/search?q=%s",
        Engine.Startpage => "https://www.startpage.com/sp/search?query=%s",
        Engine.Kagi => "https://kagi.com/search?q=%s",
        Engine.Brave => "https://search.brave.com/search?q=%s",
        Engine.Qwant => "https://www.qwant.com/?q=%s",
        _ => Accepts(Written(custom)) ? Written(custom) : Standard.Template(),
    };

    /// <summary>Suggestion endpoints the original uses (strings in the binary).</summary>
    public static string? SuggestTemplate(this Engine e) => e switch
    {
        Engine.Google => "https://suggestqueries.google.com/complete/search?client=firefox&q=%s",
        Engine.DuckDuckGo => "https://duckduckgo.com/ac/?q=%s&type=list",
        Engine.Bing => "https://www.bing.com/osjson.aspx?query=%s",
        Engine.Ecosia => "https://ac.ecosia.org/autocomplete?q=%s&type=list",
        Engine.Startpage => "https://www.startpage.com/osuggestions?q=%s",
        Engine.Kagi => "https://kagi.com/api/autosuggest?q=%s",
        Engine.Brave => "https://search.brave.com/api/suggest?q=%s",
        Engine.Qwant => "https://api.qwant.com/api/suggest/?q=%s&client=opensearch",
        _ => null,
    };

    /// <summary>A custom engine as people paste it: {searchTerms}, or no https://.</summary>
    public static string Written(string template)
    {
        var text = template.Trim().Replace("{searchTerms}", "%s");
        if (text.Length > 0 && !text.Contains("://")) text = "https://" + text;
        return text;
    }

    public static bool Accepts(string template) =>
        template.Contains("%s") && Uri.TryCreate(template.Replace("%s", "x"), UriKind.Absolute, out var u) && !string.IsNullOrEmpty(u.Host);

    public static string SearchUrl(this Engine e, string query, string custom = "") =>
        e.Template(custom).Replace("%s", Uri.EscapeDataString(query));
}
