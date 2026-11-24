using SmartSort.App.Core.Documents;

namespace SmartSort.App.Core.Extraction;

/// <summary>Extracts text without assuming a local file path. Cancellation must propagate.</summary>
public interface IDocumentTextExtractor
{
    bool CanExtract(DocumentInput document);
    Task<TextExtractionResult> ExtractAsync(DocumentInput document, CancellationToken cancellationToken = default);
}
