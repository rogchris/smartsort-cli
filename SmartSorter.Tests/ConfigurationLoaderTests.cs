using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using SmartSort.App.Configuration;
using SmartSort.App.Infrastructure;

namespace SmartSort.Tests;

public class ConfigurationLoaderTests
{
    internal static AppSettings Settings() => new()
    {
        InputFolder = TestPaths.Get("in"), DefaultTarget = TestPaths.Get("out"), LogFolder = TestPaths.Get("logs")
    };

    internal static MockFileSystem Configuration(AppSettings? settings = null)
    {
        var fs = new MockFileSystem();
        fs.AddFile(TestPaths.Get("config/appsettings.json"), new MockFileData(JsonSerializer.Serialize(settings ?? Settings())));
        fs.AddFile(TestPaths.Get("config/App/Configuration/rules.json"), new MockFileData("""{"Rules":[]} """));
        return fs;
    }

    [Fact]
    public void EnvironmentOverridesBaseAndCommandLineOverridesEnvironment()
    {
        var fs = Configuration();
        fs.AddFile(TestPaths.Get("config/appsettings.Test.json"), new MockFileData("""{"DryRun":false,"MaxFileSizeMB":2} """));
        var (settings, rules) = new ConfigurationLoader(fs).Load(TestPaths.Get("config"), TestPaths.Get("work"), ["--DryRun", "true"], "Test");
        Assert.True(settings.DryRun);
        Assert.Equal(2, settings.MaxFileSizeMB);
        Assert.Empty(rules.Rules);
        Assert.False(fs.Directory.Exists(settings.DefaultTarget));
        Assert.False(fs.Directory.Exists(settings.LogFolder));
    }

    [Fact]
    public void DefaultsAreSafeAndRelativePathsUseExplicitWorkingDirectory()
    {
        var fs = Configuration();
        fs.File.WriteAllText(TestPaths.Get("config/appsettings.json"), """{"InputFolder":"Inbox","DefaultTarget":"Sorted","LogFolder":"Logs"} """);
        var (settings, _) = new ConfigurationLoader(fs).Load(TestPaths.Get("config"), TestPaths.Get("work"), [], "Development");
        Assert.True(settings.DryRun);
        Assert.Equal(50, settings.MaxFileSizeMB);
        Assert.Equal(5, settings.MaxPdfPages);
        Assert.Equal(TestPaths.Get("work/Inbox"), settings.InputFolder);
    }

    [Theory]
    [InlineData("--MaxFileSizeMB", "0")]
    [InlineData("--MaxPdfPages", "-1")]
    [InlineData("--MaxPdfTextCharacters", "0")]
    [InlineData("--InputFolder", " ")]
    [InlineData("--DefaultTarget", " ")]
    [InlineData("--LogFolder", " ")]
    public void RejectsInvalidSettingsBeforeCreatingFolders(string key, string value)
    {
        var fs = Configuration();
        Assert.Throws<ArgumentException>(() => new ConfigurationLoader(fs).Load(TestPaths.Get("config"), TestPaths.Get("work"), [key, value]));
        Assert.False(fs.Directory.Exists(TestPaths.Get("out")));
    }

    [Theory]
    [InlineData("--MaxFileSizeMB", "abc")]
    [InlineData("--DryRun", "not-a-bool")]
    [InlineData("--DryRn", "false")]
    public void RejectsInvalidBindingAndUnknownSettings(string key, string value)
    {
        var fs = Configuration();
        Assert.Throws<InvalidOperationException>(() => new ConfigurationLoader(fs).Load(TestPaths.Get("config"), TestPaths.Get("work"), [key, value]));
    }

    [Theory]
    [InlineData("in")]
    [InlineData("in/sub")]
    [InlineData("")]
    public void RejectsOverlappingRoots(string suffix)
    {
        var settings = Settings();
        settings.DefaultTarget = TestPaths.Get(suffix);
        var fs = Configuration(settings);
        Assert.Throws<ArgumentException>(() => new ConfigurationLoader(fs).Load(TestPaths.Get("config"), TestPaths.Get("work"), []));
    }

    [Fact]
    public void RejectsMissingRuleArrayAndUnsafeEnvironmentName()
    {
        var fs = Configuration();
        var loader = new ConfigurationLoader(fs);
        Assert.Throws<ArgumentException>(() => loader.Load(TestPaths.Get("config"), TestPaths.Get("work"), [], "../escape"));
        fs.File.WriteAllText(TestPaths.Get("config/App/Configuration/rules.json"), "{}");
        Assert.Throws<JsonException>(() => loader.Load(TestPaths.Get("config"), TestPaths.Get("work"), []));
    }
    [Theory]
    [InlineData("{\"AllowedExtensions\":null}")]
    [InlineData("{\"AllowedExtensions\":{}}")]
    [InlineData("{\"AllowedExtensions\":[null]}")]
    [InlineData("{\"DryRun\":null}")]
    public void NullAndMalformedAllowListsCannotSilentlyDisableFiltering(string json)
    {
        var fs = Configuration();
        fs.AddFile(TestPaths.Get("config/appsettings.Test.json"), new MockFileData(json));
        Assert.Throws<InvalidDataException>(() => new ConfigurationLoader(fs).Load(TestPaths.Get("config"), TestPaths.Get("work"), [], "Test"));
    }
}
