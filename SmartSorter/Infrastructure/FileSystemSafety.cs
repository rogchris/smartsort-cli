using System.IO.Abstractions;
using SmartSort.App.Core;

namespace SmartSort.App.Infrastructure;

/// <summary>Local path checks. These checks do not replace OS permissions or provide protection against hostile concurrent path replacement.</summary>
public static class FileSystemSafety
{
    public static StringComparison PathComparison => OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static bool IsWithin(string root, string path)
    {
        var fullRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        return fullPath.Equals(fullRoot, PathComparison)
            || fullPath.StartsWith(Path.EndsInDirectorySeparator(fullRoot) ? fullRoot : fullRoot + Path.DirectorySeparatorChar, PathComparison);
    }

    public static string ResolveTarget(IFileSystem fileSystem, string root, string subfolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var relative = RelativeTarget.Normalize(subfolder).Replace('/', fileSystem.Path.DirectorySeparatorChar);
        var fullRoot = fileSystem.Path.GetFullPath(root);
        var target = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(fullRoot, relative));
        if (!IsWithin(fullRoot, target)) throw new ArgumentException("Target escapes the configured output folder.", nameof(subfolder));
        EnsureNoLinks(fileSystem, target);
        return target;
    }

    public static void EnsureNoLinks(IFileSystem fileSystem, string path)
    {
        var current = fileSystem.Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            if (TryGetAttributes(fileSystem, current) is { } attributes && attributes.HasFlag(FileAttributes.ReparsePoint))
                throw new IOException("Symbolic links and reparse points are not accepted: " + current);
            current = fileSystem.Path.GetDirectoryName(current);
        }
    }

    internal static FileAttributes? TryGetAttributes(IFileSystem fileSystem, string path)
    {
        try { return fileSystem.File.GetAttributes(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }
}
