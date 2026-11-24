using System.Linq;
using System.Text;
using System.IO.Abstractions.TestingHelpers;
using SmartSort.App.Infrastructure;
using Serilog;
using Xunit;

namespace SmartSort.Tests;

public class FileScannerTests
{
    [Fact]
    public void EnumerateFiles_Should_Return_Files_From_Subdirectories()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [TestPaths.Get("Inbox/root.txt")] = new MockFileData("root"),
            [TestPaths.Get("Inbox/Sub/nested.txt")] = new MockFileData("nested")
        });

        var scanner = new FileScanner(fileSystem, new LoggerConfiguration().CreateLogger());

        var files = scanner.EnumerateFiles(TestPaths.Get("Inbox"), 10).ToList();

        Assert.Equal(2, files.Count);
        Assert.Contains(TestPaths.Get("Inbox/root.txt"), files);
        Assert.Contains(TestPaths.Get("Inbox/Sub/nested.txt"), files);
    }

    [Fact]
    public void EnumerateFiles_Should_Skip_Files_Above_Size_Limit()
    {
        var largeContent = new string('x', 2 * 1024 * 1024);
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [TestPaths.Get("Inbox/small.txt")] = new MockFileData("small"),
            [TestPaths.Get("Inbox/large.txt")] = new MockFileData(Encoding.UTF8.GetBytes(largeContent))
        });

        var scanner = new FileScanner(fileSystem, new LoggerConfiguration().CreateLogger());

        var files = scanner.EnumerateFiles(TestPaths.Get("Inbox"), 1).ToList();

        Assert.Single(files);
        Assert.Equal(TestPaths.Get("Inbox/small.txt"), files[0]);
    }

    [Fact]
    public void EnumerateFiles_Should_Return_Empty_When_Root_Does_Not_Exist()
    {
        var scanner = new FileScanner(new MockFileSystem(), new LoggerConfiguration().CreateLogger());

        var files = scanner.EnumerateFiles(TestPaths.Get("Missing"), 10).ToList();

        Assert.Empty(files);
    }
}
