using System.Reflection;

namespace Search.App;

static class Version
{
    /// <summary>This build, as CI stamps it: 1.0.19.&lt;build number&gt;. Local builds are 1.0.19.0.</summary>
    public static System.Version Number { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new(1, 0, 19, 0);
    public static string Current => Number.ToString();
}
