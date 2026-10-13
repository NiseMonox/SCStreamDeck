using SCStreamDeck.Common;
using SCStreamDeck.Logging;

namespace SCStreamDeck.Services.Installation;

/// <summary>
///     Provides centralized path management for the plugin with security validation.
/// </summary>
public class PathProviderService
{
    private const string StateFileName = ".plugin-state.json";

    public PathProviderService()
    {
        string baseDirectory = AppContext.BaseDirectory;
        BaseDirectory = baseDirectory;
        CacheDirectory = Path.Combine(baseDirectory, "cache");
    }

    /// <summary>
    ///     Paths for the running plugin. Settings and generated keybindings are kept under
    ///     %LOCALAPPDATA%\&lt;plugin id&gt;\cache, because Stream Deck replaces the plugin folder when the plugin is
    ///     updated. Settings saved by older versions inside the plugin folder are moved over once. Falls back to the
    ///     plugin folder if the per-user folder cannot be used.
    /// </summary>
    public static PathProviderService CreateForPlugin()
    {
        string baseDirectory = AppContext.BaseDirectory;
        return new PathProviderService(baseDirectory, ResolveCacheDirectory(baseDirectory));
    }

    private static string ResolveCacheDirectory(string baseDirectory)
    {
        string legacyCacheDirectory = Path.Combine(baseDirectory, "cache");

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string? pluginId = typeof(PathProviderService).Assembly.GetName().Name;
        if (string.IsNullOrWhiteSpace(localAppData) || string.IsNullOrWhiteSpace(pluginId))
        {
            return legacyCacheDirectory;
        }

        string cacheDirectory = Path.Combine(localAppData, pluginId, "cache");

        try
        {
            Directory.CreateDirectory(cacheDirectory);
            MoveLegacyState(legacyCacheDirectory, cacheDirectory);
            return cacheDirectory;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"[{nameof(PathProviderService)}] Cannot use '{cacheDirectory}', keeping data in the plugin folder", ex);
            return legacyCacheDirectory;
        }
    }

    private static void MoveLegacyState(string legacyCacheDirectory, string cacheDirectory)
    {
        if (!Directory.Exists(legacyCacheDirectory))
        {
            return;
        }

        string legacyState = Path.Combine(legacyCacheDirectory, StateFileName);
        string state = Path.Combine(cacheDirectory, StateFileName);
        if (File.Exists(legacyState) && !File.Exists(state))
        {
            File.Copy(legacyState, state);
            Log.Info($"[{nameof(PathProviderService)}] Moved plugin settings to '{cacheDirectory}'");
        }

        // Everything else in the old folder is generated and will be rebuilt.
        Directory.Delete(legacyCacheDirectory, true);
    }

    /// <summary>
    ///     Protected constructor for testing with custom directories.
    /// </summary>
    protected PathProviderService(string baseDirectory, string cacheDirectory)
    {
        BaseDirectory = baseDirectory;
        CacheDirectory = cacheDirectory;
    }

    public string BaseDirectory { get; }
    public string CacheDirectory { get; }

    public string GetKeybindingJsonPath(string channel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);

        EnsureDirectoriesExist();

        string fileName = $"{channel.ToUpperInvariant()}-keybindings.json";
        return GetSecureCachePath(fileName);
    }

    public virtual void EnsureDirectoriesExist()
    {
        if (Directory.Exists(CacheDirectory))
        {
            return;
        }

        Directory.CreateDirectory(CacheDirectory);
    }

    public virtual string GetSecureCachePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        string fullPath = Path.Combine(CacheDirectory, relativePath);
        return SecurePathValidator.GetSecurePath(fullPath, CacheDirectory);
    }
}
