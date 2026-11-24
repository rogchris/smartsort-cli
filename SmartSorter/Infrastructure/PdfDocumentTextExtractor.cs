using System.Text;
using Serilog;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using SmartSort.App.Core.Documents;
using SmartSort.App.Core.Extraction;

namespace SmartSort.App.Infrastructure;

/// <summary>Extracts embedded PDF text. It does not perform OCR or execute document content.</summary>
public sealed class PdfDocumentTextExtractor : IDocumentTextExtractor
{
    private readonly ILogger _logger;
    private readonly int _maxPages;
    private readonly int _maxTextCharacters;

    public PdfDocumentTextExtractor(ILogger logger, int maxPages = 5, int maxTextCharacters = 1_000_000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (maxPages <= 0) throw new ArgumentOutOfRangeException(nameof(maxPages));
        if (maxTextCharacters <= 0) throw new ArgumentOutOfRangeException(nameof(maxTextCharacters));
        _maxPages = maxPages;
        _maxTextCharacters = maxTextCharacters;
    }

    public bool CanExtract(DocumentInput document) => document.Extension == ".pdf";

    public async Task<TextExtractionResult> ExtractAsync(DocumentInput document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        if (!CanExtract(document)) return new(TextExtractionStatus.Unsupported);

        try
        {
            await using var stream = await document.OpenReadAsync(cancellationToken).ConfigureAwait(false);

            using var pdf = PdfDocument.Open(stream, ParsingOptions.LenientParsingOff);
            var pages = Math.Min(_maxPages, pdf.NumberOfPages);
            var truncated = pages < pdf.NumberOfPages;
            var text = new StringBuilder();

            for (var page = 1; page <= pages; page++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pageText = ContentOrderTextExtractor.GetText(pdf.GetPage(page)).Trim();
                var remaining = _maxTextCharacters - text.Length;
                if (pageText.Length >= remaining)
                {
                    text.Append(pageText.AsSpan(0, remaining));
                    truncated |= pageText.Length > remaining || page < pdf.NumberOfPages;
                    break;
                }
                text.Append(pageText);
                if (text.Length < _maxTextCharacters) text.Append('\n');
            }
            cancellationToken.ThrowIfCancellationRequested();
            return TextExtractionResult.FromText(text.ToString(), truncated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PdfDocumentFormatException or InvalidOperationException or NotSupportedException or ArgumentException)
        {
            // Parser exception messages can contain document data. Log only safe diagnostic metadata.
            _logger.Warning("PDF extraction failed for {FileName}; ErrorType={ErrorType}",
                document.OriginalFileName, ex.GetType().Name);
            return new(TextExtractionStatus.Failed);
        }
    }
}
