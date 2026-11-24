using System.IO.Abstractions;
using SmartSort.App.Configuration;
using SmartSort.App.Core.Documents;

namespace SmartSort.App.Infrastructure;

/// <summary>Adapts a local path to the content contract used by classification.</summary>
public sealed class FileSystemDocumentSource
{
    private readonly IFileSystem _fs;
    private readonly long _maxBytes;
    private readonly HashSet<string> _allowedExtensions;

    public FileSystemDocumentSource(IFileSystem fileSystem, int maxFileSizeMb, IEnumerable<string>? allowedExtensions = null)
    {
        _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        if (maxFileSizeMb <= 0) throw new ArgumentOutOfRangeException(nameof(maxFileSizeMb));
        _maxBytes = maxFileSizeMb * 1024L * 1024L;
        _allowedExtensions = (allowedExtensions ?? []).Select(RuleSet.NormalizeExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public DocumentInput Create(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = _fs.Path.GetFullPath(path);
        Validate(fullPath);
        return new DocumentInput(_fs.Path.GetFileName(fullPath), token =>
        {
            token.ThrowIfCancellationRequested();
            Validate(fullPath);
            Stream stream = _fs.File.OpenRead(fullPath);
            try
            {
                if (stream.Length > _maxBytes)
                    throw new IOException("Document exceeds the configured size limit.");
                // Local open is synchronous. Future sources can use genuinely asynchronous I/O.
                return Task.FromResult(stream);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        });
    }

    public void Validate(string path)
    {
        FileSystemSafety.EnsureNoLinks(_fs, path);
        if (!_fs.File.Exists(path)) throw new FileNotFoundException("Document no longer exists.", path);
        if (_allowedExtensions.Count > 0 && !_allowedExtensions.Contains(_fs.Path.GetExtension(path)))
            throw new InvalidDataException("Document extension is not allowed.");
        if (_fs.FileInfo.New(path).Length > _maxBytes)
            throw new IOException("Document exceeds the configured size limit.");
    }
}
