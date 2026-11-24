using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using SmartSort.App.Configuration;

namespace SmartSort.App.Infrastructure;

/// <summary>Loads host settings and rule definitions, failing before scanning or creating output folders.</summary>
public sealed class ConfigurationLoader
{
    private readonly IFileSystem _fs;
    public ConfigurationLoader(IFileSystem fileSystem) => _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

    public (AppSettings Settings, RuleSet Rules) Load(string configDirectory, string pathBase, string[] args, string? environment = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathBase);
        ArgumentNullException.ThrowIfNull(args);
        if (!string.IsNullOrEmpty(environment) && environment is not ("Development" or "Test" or "Production"))
            throw new ArgumentException("SMARTSORT_ENVIRONMENT must be Development, Test or Production.", nameof(environment));

        using var baseSettings = _fs.File.OpenRead(_fs.Path.Combine(configDirectory, "appsettings.json"));
        ValidateJsonSettings(baseSettings);
        var builder = new ConfigurationBuilder().AddJsonStream(baseSettings);
        var environmentFile = string.IsNullOrEmpty(environment) ? null : _fs.Path.Combine(configDirectory, $"appsettings.{environment}.json");
        using var overrides = environmentFile is not null && _fs.File.Exists(environmentFile) ? _fs.File.OpenRead(environmentFile) : null;
        if (overrides is not null)
        {
            ValidateJsonSettings(overrides);
            builder.AddJsonStream(overrides);
        }
        builder.AddCommandLine(args);
        var configuration = builder.Build();
        using var configurationLifetime = configuration as IDisposable;
        var settings = configuration.Get<AppSettings>(options => options.ErrorOnUnknownConfiguration = true)
            ?? throw new InvalidDataException("Application settings are required.");
        ValidateAndNormalize(settings, pathBase);
        var rules = RuleSet.FromJson(_fs.File.ReadAllText(_fs.Path.Combine(configDirectory, "App", "Configuration", "rules.json")));
        return (settings, rules);
    }

    private static void ValidateJsonSettings(Stream stream)
    {
        using var json = JsonDocument.Parse(stream);
        if (json.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Application settings must be a JSON object.");
        foreach (var property in json.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Null)
                throw new InvalidDataException("Application settings must not contain null values.");
            if (property.Name.Equals(nameof(AppSettings.AllowedExtensions), StringComparison.OrdinalIgnoreCase)
                && (property.Value.ValueKind != JsonValueKind.Array
                    || property.Value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.String)))
                throw new InvalidDataException("AllowedExtensions must be an array of strings.");
        }
        stream.Position = 0;
    }

    private void ValidateAndNormalize(AppSettings settings, string pathBase)
    {
        settings.InputFolder = NormalizePath(settings.InputFolder, nameof(settings.InputFolder), pathBase);
        settings.DefaultTarget = NormalizePath(settings.DefaultTarget, nameof(settings.DefaultTarget), pathBase);
        settings.LogFolder = NormalizePath(settings.LogFolder, nameof(settings.LogFolder), pathBase);
        if (settings.MaxFileSizeMB <= 0 || settings.MaxPdfPages <= 0 || settings.MaxPdfTextCharacters <= 0)
            throw new ArgumentException("File size, PDF page and text limits must be positive.");
        if (settings.AllowedExtensions is null) throw new ArgumentException("AllowedExtensions must be an array.");
        settings.AllowedExtensions = settings.AllowedExtensions.Select(RuleSet.NormalizeExtension).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var paths = new[] { settings.InputFolder, settings.DefaultTarget, settings.LogFolder };
        for (var i = 0; i < paths.Length; i++)
        {
            FileSystemSafety.EnsureNoLinks(_fs, paths[i]);
            if (_fs.File.Exists(paths[i])) throw new ArgumentException("A configured folder is a file: " + paths[i]);
            for (var j = i + 1; j < paths.Length; j++)
                if (FileSystemSafety.IsWithin(paths[i], paths[j]) || FileSystemSafety.IsWithin(paths[j], paths[i]))
                    throw new ArgumentException("Input, output and log folders must not overlap.");
        }
    }

    private string NormalizePath(string value, string name, string pathBase)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))
            throw new ArgumentException($"{name} must contain a valid folder path.");
        // A Windows drive path must not silently become a relative Linux filename.
        if (!OperatingSystem.IsWindows() && value.Contains('\\'))
            throw new ArgumentException($"{name} uses Windows separators on a non-Windows host.");
        if (!OperatingSystem.IsWindows() && value.Contains(':'))
            throw new ArgumentException($"{name} contains an unsupported drive or URI prefix.");
        return _fs.Path.GetFullPath(value, pathBase);
    }
}
