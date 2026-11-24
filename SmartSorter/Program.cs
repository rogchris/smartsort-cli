using System.IO.Abstractions;
using System.Text.Json;
using SmartSort.App.Core;
using SmartSort.App.Core.Extraction;
using SmartSort.App.Infrastructure;
using SmartSort.App.Logging;
using SmartSort.App.Processing;

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
Console.CancelKeyPress += cancelHandler;
try
{
    var fs = new FileSystem();
    var (settings, rules) = new ConfigurationLoader(fs).Load(AppContext.BaseDirectory,
        Directory.GetCurrentDirectory(), args, Environment.GetEnvironmentVariable("SMARTSORT_ENVIRONMENT"));
    using var logger = LoggerFactory.Create(settings.LogFolder);
    var extractors = new CompositeTextExtractor([new PdfDocumentTextExtractor(logger, settings.MaxPdfPages, settings.MaxPdfTextCharacters)]);
    var classifier = new FileClassifier(new RuleEngine(rules, extractors));
    var scanner = new FileScanner(fs, logger);
    var source = new FileSystemDocumentSource(fs, settings.MaxFileSizeMB, settings.AllowedExtensions);
    var processor = new DocumentProcessor(source, classifier, new FileMover(fs), fs, settings.DefaultTarget);
    var app = new SortApplication(scanner, processor, logger, fs);
    var summary = await app.RunAsync(settings, cancellation.Token);
    return summary.ExitCode;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("Processing cancelled.");
    return 130;
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"Invalid configuration JSON (line {ex.LineNumber}, byte {ex.BytePositionInLine}).");
    return 2;
}
catch (Exception ex) when (ex is ArgumentException or InvalidDataException or FileNotFoundException or DirectoryNotFoundException)
{
    Console.Error.WriteLine($"SmartSort configuration or path error: {ex.Message}");
    return 2;
}
catch (Exception ex)
{
    // Do not dump parser messages, configuration values or document contents to the console.
    Console.Error.WriteLine($"SmartSort stopped ({ex.GetType().Name}). Check configuration, folder access and operational logs.");
    return 2;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
