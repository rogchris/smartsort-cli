using SmartSort.App.Core.Documents;

namespace SmartSort.App.Core.Extraction;

/// <summary>Tries supported extractors in registration order, allowing a later extractor to handle a textless PDF.</summary>
public sealed class CompositeTextExtractor : IDocumentTextExtractor
{
    private readonly IDocumentTextExtractor[] _extractors;

    public CompositeTextExtractor(IEnumerable<IDocumentTextExtractor> extractors)
    {
        ArgumentNullException.ThrowIfNull(extractors);
        _extractors = extractors.ToArray();
        if (_extractors.Any(e => e is null))
            throw new ArgumentException("Extractors must not contain null entries.", nameof(extractors));
    }

    public bool CanExtract(DocumentInput document) => _extractors.Any(e => e.CanExtract(document));

    public async Task<TextExtractionResult> ExtractAsync(DocumentInput document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        var status = TextExtractionStatus.Unsupported;
        var truncated = false;
        foreach (var extractor in _extractors)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!extractor.CanExtract(document))
                continue;
            var result = await extractor.ExtractAsync(document, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status == TextExtractionStatus.Succeeded && !string.IsNullOrWhiteSpace(result.Text))
                return result;
            truncated |= result.IsTruncated;
            if (result.Status == TextExtractionStatus.Failed)
                status = TextExtractionStatus.Failed;
            else if (status != TextExtractionStatus.Failed && result.Status != TextExtractionStatus.Unsupported)
                status = TextExtractionStatus.NoText;
        }
        return new TextExtractionResult(status, IsTruncated: truncated);
    }
}
