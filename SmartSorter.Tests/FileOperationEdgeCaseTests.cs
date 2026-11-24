using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Serilog;
using SmartSort.App.Infrastructure;

namespace SmartSort.Tests;

public class FileOperationEdgeCaseTests
{
    private static readonly ILogger Logger = new LoggerConfiguration().CreateLogger();
    private static string P(string name) => TestPaths.Get(name);

    [Fact]
    public void MoverSkipsMultipleFileAndDirectoryCollisionsWithoutOverwriting()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/file.pdf"), new MockFileData("new"));
        fs.AddFile(P("out/file.pdf"), new MockFileData("old"));
        fs.AddFile(P("out/file (1).pdf"), new MockFileData("older"));
        fs.AddDirectory(P("out/file (2).pdf"));
        Assert.Equal(P("out/file (3).pdf"), new FileMover(fs).Move(P("in/file.pdf"), P("out")));
        Assert.Equal("old", fs.File.ReadAllText(P("out/file.pdf")));
        Assert.Equal("older", fs.File.ReadAllText(P("out/file (1).pdf")));
        Assert.Equal("new", fs.File.ReadAllText(P("out/file (3).pdf")));
    }

    [Fact]
    public void MoverRetriesCollisionCreatedDuringMove()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/file.txt"), new MockFileData("source"));
        var raced = false;
        var file = FileSystemProxy.Wrap<IFile>(fs.File, (method, args, next) =>
        {
            if (method.Name == "Move" && !raced)
            {
                raced = true;
                fs.AddFile((string)args[1]!, new MockFileData("concurrent"));
                throw new IOException("Destination claimed by another writer");
            }
            return next();
        });
        var proxy = FileSystemProxy.Wrap<IFileSystem>(fs, (method, _, next) => method.Name == "get_File" ? file : next());
        Assert.Equal(P("out/file (1).txt"), new FileMover(proxy).Move(P("in/file.txt"), P("out")));
        Assert.Equal("concurrent", fs.File.ReadAllText(P("out/file.txt")));
    }

    [Fact]
    public void MoverPropagatesPermissionFailuresAndPreservesSource()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/file.txt"), new MockFileData("source"));
        var file = FileSystemProxy.Wrap<IFile>(fs.File, (method, _, next) =>
            method.Name == "Move" ? throw new UnauthorizedAccessException("denied") : next());
        var proxy = FileSystemProxy.Wrap<IFileSystem>(fs, (method, _, next) => method.Name == "get_File" ? file : next());
        Assert.Throws<UnauthorizedAccessException>(() => new FileMover(proxy).Move(P("in/file.txt"), P("out")));
        Assert.True(fs.File.Exists(P("in/file.txt")));
    }

    [Theory]
    [InlineData("", "out")]
    [InlineData("source", " ")]
    public void MoverRejectsInvalidInputs(string source, string destination) =>
        Assert.Throws<ArgumentException>(() => new FileMover(new MockFileSystem()).Move(source, destination));

    [Fact]
    public void MoverRejectsSelfMoveAndMissingSourceBeforeCreatingOutput()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/file.txt"), new MockFileData("source"));
        Assert.Throws<IOException>(() => new FileMover(fs).Move(P("in/file.txt"), P("in")));
        var ex = Assert.Throws<FileNotFoundException>(() => new FileMover(fs).Move(P("missing.txt"), P("new/output")));
        Assert.Equal(P("missing.txt"), ex.FileName);
        Assert.False(fs.Directory.Exists(P("new/output")));
        Assert.True(fs.File.Exists(P("in/file.txt")));
    }

    [Fact]
    public void ScannerHandlesEmptyFoldersAndInclusiveSizeLimit()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory(P("empty"));
        Assert.Empty(new FileScanner(fs, Logger).EnumerateFiles(P("empty"), 1));
        fs.AddFile(P("in/exact.bin"), new MockFileData(new byte[1024 * 1024]));
        fs.AddFile(P("in/over.bin"), new MockFileData(new byte[1024 * 1024 + 1]));
        Assert.Equal([P("in/exact.bin")], new FileScanner(fs, Logger).EnumerateFiles(P("in"), 1));
        Assert.Equal(2, new FileScanner(fs, Logger).EnumerateFiles(P("in"), 4096).Count());
    }

    [Fact]
    public void ScannerFiltersExtensionsWithoutReadingContents()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/file.PDF"), new MockFileData("pdf"));
        fs.AddFile(P("in/file.exe"), new MockFileData("exe"));
        var file = FileSystemProxy.Wrap<IFile>(fs.File, (method, _, next) =>
            method.Name == "OpenRead" ? throw new InvalidOperationException("Scanner must not read contents") : next());
        var proxy = FileSystemProxy.Wrap<IFileSystem>(fs, (method, _, next) => method.Name == "get_File" ? file : next());
        Assert.Equal([P("in/file.PDF")], new FileScanner(proxy, Logger).EnumerateFiles(P("in"), 1, ["pdf"]));
    }

    [Fact]
    public void ScannerSurvivesDeferredEnumerationFailureAndStillVisitsSubfolders()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/first.txt"), new MockFileData("first"));
        fs.AddFile(P("in/sub/second.txt"), new MockFileData("second"));
        var directory = FileSystemProxy.Wrap<IDirectory>(fs.Directory, (method, args, next) =>
            method.Name == "EnumerateFiles" && (string)args[0]! == P("in") ? BrokenEnumeration(P("in/first.txt")) : next());
        var proxy = FileSystemProxy.Wrap<IFileSystem>(fs, (method, _, next) => method.Name == "get_Directory" ? directory : next());
        var paths = new FileScanner(proxy, Logger).EnumerateFiles(P("in"), 1).ToArray();
        Assert.Equal(2, paths.Length);
        Assert.Contains(P("in/sub/second.txt"), paths);
    }

    private static IEnumerable<string> BrokenEnumeration(string first)
    {
        yield return first;
        throw new UnauthorizedAccessException("Deferred enumeration failure");
    }

    [Fact]
    public void ScannerSkipsInaccessibleChildAndContinuesSiblings()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/blocked/no.txt"), new MockFileData("no"));
        fs.AddFile(P("in/ok/yes.txt"), new MockFileData("yes"));
        var directory = FileSystemProxy.Wrap<IDirectory>(fs.Directory, (method, args, next) =>
            method.Name.StartsWith("Enumerate") && (string)args[0]! == P("in/blocked")
                ? throw new UnauthorizedAccessException() : next());
        var proxy = FileSystemProxy.Wrap<IFileSystem>(fs, (method, _, next) => method.Name == "get_Directory" ? directory : next());
        Assert.Equal([P("in/ok/yes.txt")], new FileScanner(proxy, Logger).EnumerateFiles(P("in"), 1));
    }

    [Fact]
    public void ScannerAndMoverRejectReparsePoints()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/file.txt"), new MockFileData("source"));
        fs.AddFile(P("in/link.txt"), new MockFileData("link") { Attributes = FileAttributes.ReparsePoint });
        fs.AddDirectory(P("out"));
        fs.File.SetAttributes(P("out"), FileAttributes.Directory | FileAttributes.ReparsePoint);
        Assert.Equal([P("in/file.txt")], new FileScanner(fs, Logger).EnumerateFiles(P("in"), 1));
        Assert.Throws<IOException>(() => new FileMover(fs).Move(P("in/file.txt"), P("out/child")));
        Assert.Throws<IOException>(() => new FileMover(fs).Move(P("in/link.txt"), P("safe")));
    }

    [Fact]
    public void CancellationAndInvalidLimitsAreNotSilentlyIgnored()
    {
        var fs = new MockFileSystem();
        var scanner = new FileScanner(fs, Logger);
        Assert.Throws<ArgumentOutOfRangeException>(() => scanner.EnumerateFiles(P("in"), 0).ToList());
        Assert.ThrowsAny<OperationCanceledException>(() => scanner.EnumerateFiles(P("in"), 1, cancellationToken: new(true)).ToList());
        Assert.ThrowsAny<OperationCanceledException>(() => new FileMover(fs).Move(P("in/a"), P("out"), new(true)));
    }

    [Fact]
    public async Task SourceRechecksSizeAndExistenceWhenOpened()
    {
        var fs = new MockFileSystem();
        fs.AddFile(P("in/file.pdf"), new MockFileData("short"));
        var source = new FileSystemDocumentSource(fs, 1, ["pdf"]);
        var input = source.Create(P("in/file.pdf"));
        fs.File.WriteAllBytes(P("in/file.pdf"), new byte[1024 * 1024 + 1]);
        await Assert.ThrowsAsync<IOException>(() => input.OpenReadAsync());
        fs.File.Delete(P("in/file.pdf"));
        await Assert.ThrowsAsync<FileNotFoundException>(() => input.OpenReadAsync());
        fs.AddFile(P("in/file.exe"), new MockFileData("not allowed"));
        Assert.Throws<InvalidDataException>(() => source.Create(P("in/file.exe")));
    }

    [Fact]
    public void OutputResolverKeepsNestedTargetsInsideRoot()
    {
        var fs = new MockFileSystem();
        Assert.Equal(P("out/Invoices/2026"), FileSystemSafety.ResolveTarget(fs, P("out"), "Invoices/2026"));
        Assert.Throws<ArgumentException>(() => FileSystemSafety.ResolveTarget(fs, P("out"), "../escape"));
        Assert.False(FileSystemSafety.IsWithin(P("out"), P("outside/file")));
    }
}
