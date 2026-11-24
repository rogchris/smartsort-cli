using System.Text;
using Serilog;
using SmartSort.App.Core.Documents;
using SmartSort.App.Core.Extraction;
using SmartSort.App.Infrastructure;

namespace SmartSort.Tests;

public class DocumentExtractionTests
{
    private static readonly ILogger Logger = new LoggerConfiguration().CreateLogger();

    [Theory]
    [InlineData("../secret.pdf")]
    [InlineData("folder\\secret.pdf")]
    [InlineData("C:secret.pdf")]
    [InlineData("")]
    public void InputRejectsPathsAndEmptyNames(string name) =>
        Assert.Throws<ArgumentException>(() => Input(name, []));

    [Fact]
    public async Task InputIsLazyAndCancellationDoesNotOpenContent()
    {
        var opened = false;
        var input = new DocumentInput("file.PDF", _ => { opened = true; return Task.FromResult<Stream>(new MemoryStream()); });
        Assert.Equal(".pdf", input.Extension);
        Assert.False(opened);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => input.OpenReadAsync(new CancellationToken(true)));
        Assert.False(opened);
    }

    [Fact]
    public async Task PdfReadsEmbeddedTextAndHonorsPageLimit()
    {
        var input = Input("invoice.pdf", Pdf("FIRST INVOICE", "SECOND SECRET"));
        var result = await new PdfDocumentTextExtractor(Logger, maxPages: 1).ExtractAsync(input);
        Assert.Equal(TextExtractionStatus.Succeeded, result.Status);
        Assert.Contains("FIRST INVOICE", result.Text);
        Assert.DoesNotContain("SECOND", result.Text);
        Assert.True(result.IsTruncated);
        Assert.DoesNotContain("INVOICE", result.ToString());
    }

    [Fact]
    public async Task PdfHonorsTextLimit()
    {
        var result = await new PdfDocumentTextExtractor(Logger, maxTextCharacters: 4)
            .ExtractAsync(Input("file.pdf", Pdf("ABCDEFGHIJ")));
        Assert.Equal("ABCD", result.Text);
        Assert.True(result.IsTruncated);
    }

    [Fact]
    public async Task TextlessAndZeroPagePdfsReturnNoText()
    {
        var extractor = new PdfDocumentTextExtractor(Logger);
        Assert.Equal(TextExtractionStatus.NoText, (await extractor.ExtractAsync(Input("blank.pdf", Pdf("")))).Status);
        Assert.Equal(TextExtractionStatus.NoText, (await extractor.ExtractAsync(Input("empty.pdf", Pdf()))).Status);
    }

    [Fact]
    public async Task CorruptAndZeroBytePdfsReturnFailure()
    {
        var extractor = new PdfDocumentTextExtractor(Logger);
        Assert.Equal(TextExtractionStatus.Failed, (await extractor.ExtractAsync(Input("corrupt.pdf", "NOT A PDF"u8.ToArray()))).Status);
        Assert.Equal(TextExtractionStatus.Failed, (await extractor.ExtractAsync(Input("empty.pdf", []))).Status);
    }

    [Fact]
    public async Task UnsupportedFilesAreNotOpened()
    {
        var input = new DocumentInput("photo.png", _ => throw new InvalidOperationException("Must not open"));
        Assert.Equal(TextExtractionStatus.Unsupported, (await new PdfDocumentTextExtractor(Logger).ExtractAsync(input)).Status);
    }

    [Fact]
    public async Task PdfDisposesItsInputAndPropagatesCancellation()
    {
        var stream = new MemoryStream(Pdf("hello"));
        var input = new DocumentInput("file.pdf", _ => Task.FromResult<Stream>(stream));
        var extractor = new PdfDocumentTextExtractor(Logger);
        await extractor.ExtractAsync(input);
        Assert.False(stream.CanRead);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => extractor.ExtractAsync(input, new CancellationToken(true)));
    }

    [Fact]
    public async Task PdfHandlesReadFailures()
    {
        var input = new DocumentInput("locked.pdf", _ => throw new UnauthorizedAccessException());
        Assert.Equal(TextExtractionStatus.Failed, (await new PdfDocumentTextExtractor(Logger).ExtractAsync(input)).Status);
    }

    [Fact]
    public async Task CompositeCanTryAnotherExtractorAfterTextlessPdf()
    {
        var next = new StubExtractor(TextExtractionResult.FromText("fallback text"));
        var composite = new CompositeTextExtractor([new PdfDocumentTextExtractor(Logger), next]);
        var result = await composite.ExtractAsync(Input("scan.pdf", Pdf("")));
        Assert.Equal("fallback text", result.Text);
        Assert.Equal(1, next.Calls);
    }

    [Fact]
    public async Task CompositeStopsAfterSuccessAndReportsEmptyRegistration()
    {
        var second = new StubExtractor(TextExtractionResult.FromText("second"));
        var input = Input("file.pdf", []);
        var composite = new CompositeTextExtractor([new StubExtractor(TextExtractionResult.FromText("first")), second]);
        Assert.Equal("first", (await composite.ExtractAsync(input)).Text);
        Assert.Equal(0, second.Calls);
        Assert.Equal(TextExtractionStatus.Unsupported, (await new CompositeTextExtractor([]).ExtractAsync(input)).Status);
    }

    internal static DocumentInput Input(string name, byte[] bytes) =>
        new(name, _ => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)));

    // Tiny valid PDF fixtures with embedded Helvetica text; no binary test assets or PDF writer dependencies.
    internal static byte[] Pdf(params string[] pageTexts)
    {
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(' ', pageTexts.Select((_, i) => $"{4 + i * 2} 0 R"))}] /Count {pageTexts.Length} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        for (var i = 0; i < pageTexts.Length; i++)
        {
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 300] /Resources << /Font << /F1 3 0 R >> >> /Contents {5 + i * 2} 0 R >>");
            var content = $"BT /F1 12 Tf 20 200 Td ({pageTexts[i]}) Tj ET";
            objects.Add($"<< /Length {content.Length} >>\nstream\n{content}\nendstream");
        }
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(pdf.Length);
            pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = pdf.Length;
        pdf.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets) pdf.Append($"{offset:D10} 00000 n \n");
        pdf.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    internal sealed class StubExtractor(TextExtractionResult result) : IDocumentTextExtractor
    {
        public int Calls { get; private set; }
        public bool CanExtract(DocumentInput document) => true;
        public Task<TextExtractionResult> ExtractAsync(DocumentInput document, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(result);
        }
    }
}
