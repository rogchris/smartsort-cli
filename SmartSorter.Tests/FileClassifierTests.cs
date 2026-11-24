using SmartSort.App.Core;
using static SmartSort.Tests.DocumentExtractionTests;
using static SmartSort.Tests.RuleEngineTests;

namespace SmartSort.Tests;

public class FileClassifierTests
{
    [Fact]
    public async Task RuleDecisionRetainsRuleNameAndRelativeTarget()
    {
        var result = await new FileClassifier(Engine(Rule())).ClassifyAsync(Input("invoice.pdf", []));
        Assert.Equal("Invoices", result.TargetSubfolder);
        Assert.Equal("Invoice", result.RuleName);
        Assert.False(result.UsedDefault);
    }

    [Theory]
    [InlineData("file.pdf", "PDFs")]
    [InlineData("file.PDF", "PDFs")]
    [InlineData("photo.jpg", "Images")]
    [InlineData("photo.jpeg", "Images")]
    [InlineData("photo.PNG", "Images")]
    [InlineData("file.xyz", "_Uncategorized")]
    [InlineData("file", "_Uncategorized")]
    public async Task DefaultsPreserveExistingCategories(string name, string folder)
    {
        var result = await new FileClassifier(Engine(Rule())).ClassifyAsync(Input(name, []));
        Assert.Equal(folder, result.TargetSubfolder);
        Assert.True(result.UsedDefault);
        Assert.True(result.IsClassified);
        Assert.Null(result.RuleName);
        Assert.False(Path.IsPathRooted(result.TargetSubfolder));
    }
}
