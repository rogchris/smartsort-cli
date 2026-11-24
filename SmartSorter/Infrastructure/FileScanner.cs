using System.IO.Abstractions;
using Serilog;
using SmartSort.App.Configuration;

namespace SmartSort.App.Infrastructure;

/// <summary>Enumerates local input files only; does not open content, classify or move files.</summary>
public sealed class FileScanner
{
    private readonly IFileSystem _fs;
    private readonly ILogger _logger;

    public FileScanner(IFileSystem fileSystem, ILogger logger)
    {
        _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IEnumerable<string> EnumerateFiles(string rootFolder, int maxFileSizeMb,
        IEnumerable<string>? allowedExtensions = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        if (maxFileSizeMb <= 0) throw new ArgumentOutOfRangeException(nameof(maxFileSizeMb), "A positive size limit is required.");
        var root = _fs.Path.GetFullPath(rootFolder);
        var allowed = allowedExtensions?.Select(RuleSet.NormalizeExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sizeLimit = maxFileSizeMb * 1024L * 1024L;
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsSafeDirectory(root)) yield break;
        var directories = new Stack<string>();
        directories.Push(root);
        while (directories.TryPop(out var folder))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSafeDirectory(folder)) continue;
            foreach (var path in EnumerateSafely(() => _fs.Directory.EnumerateFiles(folder), folder, cancellationToken))
            {
                if (allowed is { Count: > 0 } && !allowed.Contains(_fs.Path.GetExtension(path))) continue;
                bool accepted;
                try
                {
                    FileSystemSafety.EnsureNoLinks(_fs, path);
                    var size = _fs.FileInfo.New(path).Length;
                    accepted = size <= sizeLimit;
                    if (!accepted) _logger.Information("File skipped: {File}; Size={Size}; Limit={Limit}", path, size, sizeLimit);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.Warning("File inaccessible: {File}; ErrorType={ErrorType}", path, ex.GetType().Name);
                    continue;
                }
                if (accepted) yield return path;
            }
            foreach (var path in EnumerateSafely(() => _fs.Directory.EnumerateDirectories(folder), folder, cancellationToken))
                directories.Push(path);
        }
    }

    private bool IsSafeDirectory(string path)
    {
        try
        {
            FileSystemSafety.EnsureNoLinks(_fs, path);
            if (!_fs.Directory.Exists(path))
            {
                _logger.Warning("Scan directory does not exist: {Folder}", path);
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warning("Directory skipped: {Folder}; ErrorType={ErrorType}", path, ex.GetType().Name);
            return false;
        }
    }

    private IEnumerable<string> EnumerateSafely(Func<IEnumerable<string>> enumerate, string folder, CancellationToken token)
    {
        IEnumerator<string> enumerator;
        try { enumerator = enumerate().GetEnumerator(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.Warning("Cannot enumerate {Folder}; ErrorType={ErrorType}", folder, ex.GetType().Name);
            yield break;
        }
        using (enumerator)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                string current;
                try
                {
                    if (!enumerator.MoveNext()) break;
                    current = enumerator.Current;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.Warning("Enumeration interrupted: {Folder}; ErrorType={ErrorType}", folder, ex.GetType().Name);
                    break;
                }
                yield return current;
            }
        }
    }
}
