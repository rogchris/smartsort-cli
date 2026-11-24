using SmartSort.App.Core.Extraction;

namespace SmartSort.App.Core;

/// <summary>A classification decision, containing no physical storage path or document contents.</summary>
public sealed record ClassificationResult(
    string? RuleName,
    string? TargetSubfolder,
    TextExtractionStatus ExtractionStatus = TextExtractionStatus.NotRequested,
    bool TextWasTruncated = false)
{
    public bool IsClassified => TargetSubfolder is not null;
    public bool IsRuleMatch => RuleName is not null;
    public bool UsedDefault => IsClassified && !IsRuleMatch;
}
