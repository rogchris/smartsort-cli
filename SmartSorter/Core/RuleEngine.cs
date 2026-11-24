using SmartSort.App.Configuration;
using SmartSort.App.Core.Documents;
using SmartSort.App.Core.Extraction;

namespace SmartSort.App.Core;

/// <summary>Deterministic rule evaluation, independent of PDF libraries, logging and physical output.</summary>
public sealed class RuleEngine
{
    private readonly Rule[] _rules;
    private readonly IDocumentTextExtractor _textExtractor;

    public RuleEngine(RuleSet ruleSet, IDocumentTextExtractor textExtractor)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);
        _rules = ruleSet.CreateSnapshot();
        _textExtractor = textExtractor ?? throw new ArgumentNullException(nameof(textExtractor));
    }

    /// <summary>Higher priority wins; equal priorities use ordinal rule-name order. OR within each field, AND between fields.</summary>
    public async Task<ClassificationResult> MatchAsync(DocumentInput document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        TextExtractionResult? extracted = null;
        var fileName = Path.GetFileNameWithoutExtension(document.OriginalFileName);
        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rule.AppliesToExtensions.Length > 0
                && !rule.AppliesToExtensions.Contains(document.Extension, StringComparer.OrdinalIgnoreCase)) continue;
            if (!MatchesTerms(rule.FileNameContains, fileName)) continue;

            if (rule.ContentContains.Length > 0)
            {
                extracted ??= _textExtractor.CanExtract(document)
                    ? await _textExtractor.ExtractAsync(document, cancellationToken).ConfigureAwait(false)
                    : new TextExtractionResult(TextExtractionStatus.Unsupported);
                cancellationToken.ThrowIfCancellationRequested();
                if (extracted.Status != TextExtractionStatus.Succeeded || !MatchesTerms(rule.ContentContains, extracted.Text)) continue;
            }
            return new(rule.Name, rule.TargetSubfolder, extracted?.Status ?? TextExtractionStatus.NotRequested,
                extracted?.IsTruncated ?? false);
        }
        return new(null, null, extracted?.Status ?? TextExtractionStatus.NotRequested, extracted?.IsTruncated ?? false);
    }

    private static bool MatchesTerms(string[] terms, string value) =>
        terms.Length == 0 || terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}
