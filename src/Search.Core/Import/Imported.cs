namespace Search.Core.Import;

public sealed record ImportedBookmark(string Title, string Url, string? Folder);
public sealed record ImportedVisit(string Url, string Title, int Count, DateTime Last);
public sealed record ImportedLogin(string Url, string User, string Password);

/// <summary>What a read found, and what it could not do (a password store that needs a primary password, a file that is locked).</summary>
public sealed class ImportRead
{
    public List<ImportedBookmark> Bookmarks { get; } = [];
    public List<ImportedVisit> Visits { get; } = [];
    public List<ImportedLogin> Logins { get; } = [];
    public List<string> Notes { get; } = [];
}
