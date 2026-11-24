using System.Text.Json;
using System.Text.Json.Serialization;

namespace Search.Core;

/// <summary>
/// Where Search keeps its files: %LOCALAPPDATA%\Search on Windows
/// (the original keeps them in ~/Library/Application Support/Search).
/// Writes go to a temp file first and are moved over, so a crash never leaves half a file.
/// </summary>
public static class Store
{
    public static string Root { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Search");

    public static string File(string name)
    {
        Directory.CreateDirectory(Root);
        return Path.Combine(Root, name);
    }

    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static T? Read<T>(string name)
    {
        try
        {
            var path = File(name);
            if (!System.IO.File.Exists(path)) return default;
            using var s = System.IO.File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(s, Json);
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            return default;
        }
    }

    public static void Write<T>(string name, T value)
    {
        var path = File(name);
        var temp = path + ".tmp";
        System.IO.File.WriteAllText(temp, JsonSerializer.Serialize(value, Json));
        System.IO.File.Move(temp, path, overwrite: true);
    }
}
