using System.IO.Abstractions;
using SmartSort.App.Core;
using SmartSort.App.Infrastructure;

namespace SmartSort.App.Processing;

/// <summary>Orchestrates one local document. The reusable classifier never sees storage paths.</summary>
public sealed class DocumentProcessor
{
    private readonly FileSystemDocumentSource _source;
    private readonly FileClassifier _classifier;
    private readonly FileMover _mover;
    private readonly IFileSystem _fs;
    private readonly string _outputRoot;

    public DocumentProcessor(FileSystemDocumentSource source, FileClassifier classifier, FileMover mover,
        IFileSystem fileSystem, string outputRoot)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _mover = mover ?? throw new ArgumentNullException(nameof(mover));
        _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        _outputRoot = _fs.Path.GetFullPath(outputRoot);
    }

    public async Task<ProcessingResult> ProcessAsync(string sourcePath, bool dryRun, CancellationToken cancellationToken = default)
    {
        ClassificationResult? classification = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = _source.Create(sourcePath);
            classification = await _classifier.ClassifyAsync(document, cancellationToken).ConfigureAwait(false);
            var target = FileSystemSafety.ResolveTarget(_fs, _outputRoot, classification.TargetSubfolder!);
            cancellationToken.ThrowIfCancellationRequested();
            _source.Validate(sourcePath);
            if (dryRun) return new(sourcePath, classification, ProcessingOutcome.Simulated);
            var destination = _mover.Move(sourcePath, target, cancellationToken);
            return new(sourcePath, classification, ProcessingOutcome.Moved, destination);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            return new(sourcePath, classification, ProcessingOutcome.Failed, ErrorType: ex.GetType().Name);
        }
    }
}
