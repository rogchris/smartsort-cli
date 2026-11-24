namespace SmartSort.App.Configuration;

/// <summary>Local console-host settings. All limits are enforced before processing.</summary>
public sealed class AppSettings
{
    public string InputFolder { get; set; } = string.Empty;
    public string DefaultTarget { get; set; } = string.Empty;
    public string LogFolder { get; set; } = string.Empty;
    public int MaxFileSizeMB { get; set; } = 50;
    public bool DryRun { get; set; } = true;
    /// <summary>Empty permits all suffixes for backward compatibility. Suffixes do not prove file format.</summary>
    public string[] AllowedExtensions { get; set; } = [];
    public int MaxPdfPages { get; set; } = 5;
    public int MaxPdfTextCharacters { get; set; } = 1_000_000;
}
