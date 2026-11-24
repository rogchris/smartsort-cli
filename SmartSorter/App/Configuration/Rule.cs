namespace SmartSort.App.Configuration;

/// <summary>Configurable rule: OR within populated arrays, AND between fields.</summary>
public sealed class Rule
{
    /// <summary>Unique name, also used as the ordinal tie-breaker for equal priorities.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Portable relative output category; validated before use.</summary>
    public string TargetSubfolder { get; set; } = string.Empty;
    public string[] AppliesToExtensions { get; set; } = [];
    /// <summary>Substrings matched against the original filename without its extension.</summary>
    public string[] FileNameContains { get; set; } = [];
    /// <summary>Substrings matched against extracted text; an empty array requires no extraction.</summary>
    public string[] ContentContains { get; set; } = [];
    /// <summary>Higher values win. Negative priorities are allowed.</summary>
    public int Priority { get; set; }
}
