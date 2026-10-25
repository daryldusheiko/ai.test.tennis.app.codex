using System.Reflection;

namespace SocialTennis;

/// <summary>
/// Provides deployment metadata embedded before assets are hashed.
/// </summary>
public static class BuildDetails
{
    /// <summary>
    /// Gets the deployment version.
    /// </summary>
    public static string Version => Read("DisplayVersion");

    /// <summary>
    /// Gets the short source commit.
    /// </summary>
    public static string Commit => Read("Commit");

    /// <summary>
    /// Gets the release date or the local-build label.
    /// </summary>
    public static string Release => Read("ReleaseDate") is "Local build" ? "Local build" : $"Released {Read("ReleaseDate")}";

    private static string Read(string key)
    {
        return typeof(BuildDetails).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(attribute => attribute.Key == key).Value ?? string.Empty;
    }
}
