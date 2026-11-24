using System.IO.Abstractions;
using Serilog;
using SmartSort.App.Configuration;
using SmartSort.App.Infrastructure;

namespace SmartSort.App.Processing;

/// <summary>Runs the local batch and owns operational logging and exit-status accounting.</summary>
public sealed class SortApplication
{
    private readonly FileScanner _scanner;
    private readonly DocumentProcessor _processor;
    private readonly ILogger _logger;
    private readonly IFileSystem _fs;

    public SortApplication(FileScanner scanner, DocumentProcessor processor, ILogger logger, IFileSystem fileSystem)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<ProcessingSummary> RunAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();
        FileSystemSafety.EnsureNoLinks(_fs, settings.InputFolder);
        if (!_fs.Directory.Exists(settings.InputFolder))
            throw new DirectoryNotFoundException("Input folder does not exist: " + settings.InputFolder);
        var discovered = 0;
        var moved = 0;
        var simulated = 0;
        var failed = 0;
        _logger.Information("Processing started; DryRun={DryRun}", settings.DryRun);
        foreach (var path in _scanner.EnumerateFiles(settings.InputFolder, settings.MaxFileSizeMB, settings.AllowedExtensions, cancellationToken))
        {
            discovered++;
            var result = await _processor.ProcessAsync(path, settings.DryRun, cancellationToken).ConfigureAwait(false);
            switch (result.Outcome)
            {
                case ProcessingOutcome.Moved: moved++; break;
                case ProcessingOutcome.Simulated: simulated++; break;
                case ProcessingOutcome.Failed: failed++; break;
            }
            if (result.Outcome == ProcessingOutcome.Failed)
                _logger.Error("Document processing failed: {File}; ErrorType={ErrorType}", path, result.ErrorType);
            else
                _logger.Information("Document processed: {File}; Outcome={Outcome}; Rule={Rule}; Category={Category}; Extraction={Extraction}; TextTruncated={TextTruncated}; Destination={Destination}",
                    path, result.Outcome, result.Classification?.RuleName ?? "Default", result.Classification?.TargetSubfolder,
                    result.Classification?.ExtractionStatus, result.Classification?.TextWasTruncated, result.DestinationPath);
        }
        var summary = new ProcessingSummary(discovered, moved, simulated, failed);
        _logger.Information("Processing completed; Discovered={Discovered}; Moved={Moved}; Simulated={Simulated}; Failed={Failed}", discovered, moved, simulated, failed);
        return summary;
    }
}
