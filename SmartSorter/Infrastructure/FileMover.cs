using System.IO.Abstractions;
using SmartSort.App.Core;

namespace SmartSort.App.Infrastructure;

/// <summary>Moves local files without overwriting existing entries. Classification and logging belong to the caller.</summary>
public sealed class FileMover
{
    private readonly IFileSystem _fs;
    public FileMover(IFileSystem fileSystem) => _fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

    public string Move(string sourcePath, string destinationFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationFolder);
        cancellationToken.ThrowIfCancellationRequested();
        var source = _fs.Path.GetFullPath(sourcePath);
        var folder = _fs.Path.GetFullPath(destinationFolder);
        FileSystemSafety.EnsureNoLinks(_fs, source);
        if (!_fs.File.Exists(source)) throw new FileNotFoundException("Source file not found.", source);
        var fileName = _fs.Path.GetFileName(source);
        RelativeTarget.Normalize(fileName);
        if (string.Equals(source, _fs.Path.Combine(folder, fileName), FileSystemSafety.PathComparison))
            throw new IOException("Source and destination refer to the same file.");
        FileSystemSafety.EnsureNoLinks(_fs, folder);
        _fs.Directory.CreateDirectory(folder);

        var name = _fs.Path.GetFileNameWithoutExtension(fileName);
        var extension = _fs.Path.GetExtension(fileName);
        for (var suffix = 0; suffix < 10_000; suffix++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileSystemSafety.EnsureNoLinks(_fs, folder);
            var target = _fs.Path.Combine(folder, suffix == 0 ? fileName : $"{name} ({suffix}){extension}");
            if (FileSystemSafety.TryGetAttributes(_fs, target) is not null) continue;
            try
            {
                _fs.File.Move(source, target, overwrite: false);
                return target;
            }
            catch (IOException) when (_fs.File.Exists(source) && FileSystemSafety.TryGetAttributes(_fs, target) is not null)
            {
                // A competing writer claimed this name after the check. Retry without overwriting.
            }
        }
        throw new IOException("No available destination name after 10000 collision attempts.");
    }
}
