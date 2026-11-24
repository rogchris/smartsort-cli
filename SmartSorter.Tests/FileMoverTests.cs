using System.IO.Abstractions.TestingHelpers;
using SmartSort.App.Infrastructure;
using Serilog;
using Xunit;

namespace SmartSort.Tests;

public class FileMoverTests
{
    [Fact]
    public void Move_Should_Move_File_To_Target_Folder()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [TestPaths.Get("Inbox/invoice.pdf")] = new MockFileData("content")
        });

        var mover = new FileMover(fileSystem);

        var result = mover.Move(TestPaths.Get("Inbox/invoice.pdf"), TestPaths.Get("Sorted/Invoices"));

        Assert.Equal(TestPaths.Get("Sorted/Invoices/invoice.pdf"), result);
        Assert.False(fileSystem.File.Exists(TestPaths.Get("Inbox/invoice.pdf")));
        Assert.True(fileSystem.File.Exists(TestPaths.Get("Sorted/Invoices/invoice.pdf")));
    }

    [Fact]
    public void Move_Should_Create_Unique_Name_When_Target_Already_Exists()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [TestPaths.Get("Inbox/invoice.pdf")] = new MockFileData("new"),
            [TestPaths.Get("Sorted/Invoices/invoice.pdf")] = new MockFileData("existing")
        });

        var mover = new FileMover(fileSystem);

        var result = mover.Move(TestPaths.Get("Inbox/invoice.pdf"), TestPaths.Get("Sorted/Invoices"));

        Assert.Equal(TestPaths.Get("Sorted/Invoices/invoice (1).pdf"), result);
        Assert.True(fileSystem.File.Exists(result));
        Assert.True(fileSystem.File.Exists(TestPaths.Get("Sorted/Invoices/invoice.pdf")));
    }

    [Fact]
    public void Move_Should_Throw_When_Source_File_Does_Not_Exist()
    {
        var mover = new FileMover(new MockFileSystem());

        Assert.Throws<FileNotFoundException>(() =>
            mover.Move(TestPaths.Get("Inbox/missing.pdf"), TestPaths.Get("Sorted")));
    }
}
