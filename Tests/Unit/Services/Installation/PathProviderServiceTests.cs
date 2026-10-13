using FluentAssertions;
using SCStreamDeck.Services.Installation;
using System.Reflection;

namespace Tests.Unit.Services.Installation;

public sealed class PathProviderServiceTests
{
    [Fact]
    public void GetKeybindingJsonPath_UppercasesChannel()
    {
        string baseDir = CreateTempDirectory();
        string cacheDir = Path.Combine(baseDir, "cache");
        PathProviderService service = new TestPathProviderService(baseDir, cacheDir);

        string path = service.GetKeybindingJsonPath("live");

        Path.GetFileName(path).Should().Be("LIVE-keybindings.json");
    }

    [Fact]
    public void EnsureDirectoriesExist_CreatesCacheDirectory()
    {
        string baseDir = CreateTempDirectory();
        string cacheDir = Path.Combine(baseDir, "cache");
        PathProviderService service = new TestPathProviderService(baseDir, cacheDir);
        string actualCacheDir = service.CacheDirectory;
        if (Directory.Exists(actualCacheDir))
        {
            Directory.Delete(actualCacheDir, true);
        }

        service.EnsureDirectoriesExist();

        Directory.Exists(actualCacheDir).Should().BeTrue();
    }

    [Fact]
    public void GetSecureCachePath_ReturnsPathWithinCache()
    {
        string baseDir = CreateTempDirectory();
        string cacheDir = Path.Combine(baseDir, "cache");
        PathProviderService service = new TestPathProviderService(baseDir, cacheDir);
        service.EnsureDirectoriesExist();

        string relative = Path.Combine("sub", "file.txt");

        string fullPath = service.GetSecureCachePath(relative);

        fullPath.Should().StartWith(service.CacheDirectory, "Path should be under cache directory");
    }

    [Fact]
    public void MoveLegacyState_MovesSettingsOutOfThePluginFolder_WithoutOverwritingNewerOnes()
    {
        static void MoveLegacyState(string legacyCacheDirectory, string cacheDirectory) =>
            typeof(PathProviderService)
                .GetMethod("MoveLegacyState", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, [legacyCacheDirectory, cacheDirectory]);

        string root = CreateTempDirectory();
        string legacy = Path.Combine(root, "plugin", "cache");
        string current = Path.Combine(root, "localappdata", "cache");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(current);
        File.WriteAllText(Path.Combine(legacy, ".plugin-state.json"), "legacy settings");
        File.WriteAllText(Path.Combine(legacy, "LIVE-keybindings.json"), "generated");

        MoveLegacyState(legacy, current);

        File.ReadAllText(Path.Combine(current, ".plugin-state.json")).Should().Be("legacy settings");
        File.Exists(Path.Combine(current, "LIVE-keybindings.json")).Should().BeFalse("generated files are rebuilt");
        Directory.Exists(legacy).Should().BeFalse();

        // A later leftover must not replace the settings that are already in the new location.
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, ".plugin-state.json"), "stale settings");

        MoveLegacyState(legacy, current);

        File.ReadAllText(Path.Combine(current, ".plugin-state.json")).Should().Be("legacy settings");
        Directory.Exists(legacy).Should().BeFalse();
    }

    private static string CreateTempDirectory()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        return tempDir;
    }

    private sealed class TestPathProviderService : PathProviderService
    {
        public TestPathProviderService(string baseDirectory, string cacheDirectory) : base(baseDirectory, cacheDirectory)
        {
        }
    }
}
