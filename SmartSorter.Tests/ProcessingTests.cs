using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using SmartSort.App.Core;
using SmartSort.App.Core.Extraction;
using SmartSort.App.Infrastructure;
using SmartSort.App.Processing;
using static SmartSort.Tests.RuleEngineTests;

namespace SmartSort.Tests;

public class ProcessingTests
{
    private static readonly ILogger Logger = new LoggerConfiguration().CreateLogger();
    private static DocumentProcessor Processor(IFileSystem fs, FileClassifier? classifier = null) => new(
        new FileSystemDocumentSource(fs, 1), classifier ?? new FileClassifier(Engine()), new FileMover(fs), fs, TestPaths.Get("out"));

    [Fact]
    public async Task DryRunPreservesSourceAndDoesNotCreateTargetDirectories()
    {
        var fs = new MockFileSystem();
        fs.AddFile(TestPaths.Get("in/invoice.pdf"), new MockFileData("source"));
        var result = await Processor(fs, new FileClassifier(Engine(Rule()))).ProcessAsync(TestPaths.Get("in/invoice.pdf"), true);
        Assert.Equal(ProcessingOutcome.Simulated, result.Outcome);
        Assert.Equal("Invoices", result.Classification!.TargetSubfolder);
        Assert.Null(result.DestinationPath);
        Assert.True(fs.File.Exists(TestPaths.Get("in/invoice.pdf")));
        Assert.False(fs.Directory.Exists(TestPaths.Get("out")));
    }

    [Fact]
    public async Task RealProcessingMovesAndRecordsActualCollisionName()
    {
        var fs = new MockFileSystem();
        fs.AddFile(TestPaths.Get("in/file.pdf"), new MockFileData("new"));
        fs.AddFile(TestPaths.Get("out/PDFs/file.pdf"), new MockFileData("old"));
        var result = await Processor(fs).ProcessAsync(TestPaths.Get("in/file.pdf"), false);
        Assert.Equal(ProcessingOutcome.Moved, result.Outcome);
        Assert.Equal(TestPaths.Get("out/PDFs/file (1).pdf"), result.DestinationPath);
        Assert.False(fs.File.Exists(TestPaths.Get("in/file.pdf")));
    }

    [Fact]
    public async Task MissingSourceProducesFailureAndCancellationPropagates()
    {
        var processor = Processor(new MockFileSystem());
        Assert.Equal(ProcessingOutcome.Failed, (await processor.ProcessAsync(TestPaths.Get("missing.pdf"), false)).Outcome);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processor.ProcessAsync(TestPaths.Get("file.pdf"), false, new(true)));
    }

    [Fact]
    public async Task BatchContinuesAfterFileFailureAndReturnsNonzeroExitCode()
    {
        var fs = new MockFileSystem();
        fs.AddFile(TestPaths.Get("in/bad.txt"), new MockFileData("bad"));
        fs.AddFile(TestPaths.Get("in/good.txt"), new MockFileData("good"));
        var file = FileSystemProxy.Wrap<IFile>(fs.File, (method, args, next) =>
            method.Name == "Move" && ((string)args[0]!).EndsWith("bad.txt") ? throw new IOException("locked") : next());
        var proxy = FileSystemProxy.Wrap<IFileSystem>(fs, (method, _, next) => method.Name == "get_File" ? file : next());
        var settings = ConfigurationLoaderTests.Settings();
        settings.DryRun = false;
        var summary = await new SortApplication(new FileScanner(proxy, Logger), Processor(proxy), Logger, proxy).RunAsync(settings);
        Assert.Equal(new ProcessingSummary(2, 1, 0, 1), summary);
        Assert.Equal(1, summary.ExitCode);
        Assert.True(fs.File.Exists(TestPaths.Get("in/bad.txt")));
        Assert.True(fs.File.Exists(TestPaths.Get("out/_Uncategorized/good.txt")));
    }

    [Fact]
    public async Task MissingInputIsNotCreatedAndDoesNotReportSuccess()
    {
        var fs = new MockFileSystem();
        var app = new SortApplication(new FileScanner(fs, Logger), Processor(fs), Logger, fs);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => app.RunAsync(ConfigurationLoaderTests.Settings()));
        Assert.False(fs.Directory.Exists(TestPaths.Get("in")));
    }

    [Fact]
    public async Task NormalLogsContainDecisionsWithoutDocumentContents()
    {
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var fs = new MockFileSystem();
        fs.AddFile(TestPaths.Get("in/invoice.pdf"), new MockFileData("private medical content"));
        var rule = Rule();
        rule.ContentContains = ["private medical content"];
        var classifier = new FileClassifier(new RuleEngine(new() { Rules = [rule] },
            new DocumentExtractionTests.StubExtractor(TextExtractionResult.FromText("private medical content"))));
        var app = new SortApplication(new FileScanner(fs, logger), Processor(fs, classifier), logger, fs);
        var summary = await app.RunAsync(ConfigurationLoaderTests.Settings());
        Assert.Equal(0, summary.ExitCode);
        Assert.Equal(1, summary.Simulated);
        Assert.Single(sink.Events, e => e.MessageTemplate.Text.StartsWith("Document processed"));
        Assert.DoesNotContain("private medical content", string.Join('\n', sink.Events.Select(e => e.RenderMessage())));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisallowedSourceExtensionProducesFileFailure(bool dryRun)
    {
        var fs = new MockFileSystem();
        var path = TestPaths.Get("in/file.txt");
        fs.AddFile(path, new MockFileData("source"));
        var processor = new DocumentProcessor(new FileSystemDocumentSource(fs, 1, [".pdf"]),
            new FileClassifier(Engine()), new FileMover(fs), fs, TestPaths.Get("out"));

        var result = await processor.ProcessAsync(path, dryRun);

        Assert.Equal(ProcessingOutcome.Failed, result.Outcome);
        Assert.Equal(nameof(InvalidDataException), result.ErrorType);
        Assert.Equal("source", fs.File.ReadAllText(path));
        Assert.False(fs.Directory.Exists(TestPaths.Get("out")));
    }

    [Fact]
    public async Task CancellationDuringFinalEnumerationDoesNotReportCompletion()
    {
        using var cancellation = new CancellationTokenSource();
        var sink = new CollectingSink();
        using var logger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger();
        var fs = new MockFileSystem();
        fs.AddDirectory(TestPaths.Get("in"));
        var directory = FileSystemProxy.Wrap<IDirectory>(fs.Directory, (method, _, next) =>
            method.Name == nameof(IDirectory.EnumerateDirectories) ? FinishEnumeration() : next());
        var proxy = FileSystemProxy.Wrap<IFileSystem>(fs, (method, _, next) =>
            method.Name == "get_Directory" ? directory : next());
        var app = new SortApplication(new FileScanner(proxy, logger), Processor(proxy), logger, proxy);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            app.RunAsync(ConfigurationLoaderTests.Settings(), cancellation.Token));
        Assert.DoesNotContain(sink.Events, e => e.MessageTemplate.Text.StartsWith("Processing completed"));

        IEnumerable<string> FinishEnumeration()
        {
            cancellation.Cancel();
            yield break;
        }
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
