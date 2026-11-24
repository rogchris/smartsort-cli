using SmartSort.App.Core.Documents;

namespace SmartSort.App.Core;

/// <summary>Combines rule evaluation with the existing default categories. Does not construct or change physical paths.</summary>
public sealed class FileClassifier
{
    private readonly RuleEngine _ruleEngine;

    public FileClassifier(RuleEngine ruleEngine) =>
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));

    public async Task<ClassificationResult> ClassifyAsync(DocumentInput document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var result = await _ruleEngine.MatchAsync(document, cancellationToken).ConfigureAwait(false);
        if (result.IsRuleMatch) return result;
        var folder = document.Extension switch
        {
            ".pdf" => "PDFs",
            ".jpg" or ".jpeg" or ".png" => "Images",
            _ => "_Uncategorized"
        };
        return result with { TargetSubfolder = folder };
    }
}
