using SmartSort.App.Configuration;
using SmartSort.App.Core;
using SmartSort.App.Core.Extraction;
using static SmartSort.Tests.DocumentExtractionTests;

namespace SmartSort.Tests;

public class RuleEngineTests
{
    internal static Rule Rule(string name = "Invoice", int priority = 10) => new()
    {
        Name = name, TargetSubfolder = "Invoices", AppliesToExtensions = [".pdf"],
        FileNameContains = ["invoice"], Priority = priority
    };

    internal static RuleEngine Engine(params Rule[] rules) =>
        new(new RuleSet { Rules = rules.ToList() }, new CompositeTextExtractor([]));

    [Fact]
    public async Task MatchesExtensionAndFileNameIgnoringCase()
    {
        var rule = Rule();
        rule.AppliesToExtensions = [" PDF "];
        var result = await Engine(rule).MatchAsync(Input("My_INVOICE.PDF", []));
        Assert.True(result.IsRuleMatch);
        Assert.True(result.IsClassified);
        Assert.Equal("Invoice", result.RuleName);
        Assert.Equal("Invoices", result.TargetSubfolder);
        Assert.Equal(TextExtractionStatus.NotRequested, result.ExtractionStatus);
    }

    [Theory]
    [InlineData("invoice.txt")]
    [InlineData("notes.pdf")]
    [InlineData("invoice.pdf.exe")]
    public async Task NonMatchingInputsHaveNoDecision(string fileName)
    {
        var result = await Engine(Rule()).MatchAsync(Input(fileName, []));
        Assert.False(result.IsClassified);
        Assert.Null(result.RuleName);
        Assert.Null(result.TargetSubfolder);
    }

    [Fact]
    public async Task EmptyRulesReturnNoMatch() =>
        Assert.False((await Engine().MatchAsync(Input("file.pdf", []))).IsRuleMatch);

    [Fact]
    public async Task PriorityWinsInEveryLoadOrder()
    {
        var low = Rule("Low", -20);
        var high = Rule("High", 100);
        foreach (var rules in new[] { new[] { low, high }, new[] { high, low } })
            Assert.Equal("High", (await Engine(rules).MatchAsync(Input("invoice.pdf", []))).RuleName);
    }

    [Fact]
    public async Task EqualPrioritiesUseOrdinalNameOrderInEveryLoadOrder()
    {
        var a = Rule("Alpha");
        var z = Rule("Zulu");
        foreach (var rules in new[] { new[] { a, z }, new[] { z, a } })
            Assert.Equal("Alpha", (await Engine(rules).MatchAsync(Input("invoice.pdf", []))).RuleName);
    }

    [Fact]
    public async Task EngineUsesSnapshotEvenIfConfigurationIsChanged()
    {
        var rule = Rule();
        var rules = new RuleSet { Rules = [rule] };
        var engine = new RuleEngine(rules, new CompositeTextExtractor([]));
        rule.Name = "Changed";
        rule.AppliesToExtensions[0] = ".txt";
        rules.Rules.Clear();
        Assert.Equal("Invoice", (await engine.MatchAsync(Input("invoice.pdf", []))).RuleName);
    }

    [Fact]
    public async Task ContentIsReadOnceAndOrTermsAreCombinedWithAndFields()
    {
        var first = Rule("First", 100);
        first.ContentContains = ["absent"];
        var next = Rule("Next", 50);
        next.ContentContains = ["absent", "PAYMENT"];
        var extractor = new StubExtractor(TextExtractionResult.FromText("payment due"));
        var engine = new RuleEngine(new RuleSet { Rules = [first, next] }, extractor);
        Assert.Equal("Next", (await engine.MatchAsync(Input("invoice.pdf", []))).RuleName);
        Assert.Equal(1, extractor.Calls);
    }

    [Theory]
    [InlineData("notes.pdf")]
    [InlineData("invoice.txt")]
    public async Task ContentIsNotReadWhenCheapFiltersFail(string name)
    {
        var rule = Rule();
        rule.ContentContains = ["payment"];
        var extractor = new StubExtractor(TextExtractionResult.FromText("payment"));
        var engine = new RuleEngine(new RuleSet { Rules = [rule] }, extractor);
        Assert.False((await engine.MatchAsync(Input(name, []))).IsRuleMatch);
        Assert.Equal(0, extractor.Calls);
    }

    [Fact]
    public async Task WinningMetadataRuleAvoidsLowerPriorityContentExtraction()
    {
        var content = Rule("Content", 1);
        content.ContentContains = ["payment"];
        var extractor = new StubExtractor(TextExtractionResult.FromText("payment"));
        var engine = new RuleEngine(new RuleSet { Rules = [content, Rule("Name", 100)] }, extractor);
        Assert.Equal("Name", (await engine.MatchAsync(Input("invoice.pdf", []))).RuleName);
        Assert.Equal(0, extractor.Calls);
    }

    [Theory]
    [InlineData(TextExtractionStatus.Failed)]
    [InlineData(TextExtractionStatus.NoText)]
    [InlineData(TextExtractionStatus.Unsupported)]
    public async Task MissingContentCannotMatchButOutcomeIsPreserved(TextExtractionStatus status)
    {
        var rule = Rule();
        rule.ContentContains = ["payment"];
        var engine = new RuleEngine(new RuleSet { Rules = [rule] }, new StubExtractor(new(status)));
        var result = await engine.MatchAsync(Input("invoice.pdf", []));
        Assert.False(result.IsRuleMatch);
        Assert.Equal(status, result.ExtractionStatus);
    }

    [Fact]
    public async Task ContentExtractionWorksForAnotherFormatWithoutEngineChanges()
    {
        var rule = Rule();
        rule.AppliesToExtensions = [".docx"];
        rule.ContentContains = ["payment"];
        var engine = new RuleEngine(new RuleSet { Rules = [rule] }, new StubExtractor(TextExtractionResult.FromText("payment")));
        Assert.True((await engine.MatchAsync(Input("invoice.docx", []))).IsRuleMatch);
    }

    [Fact]
    public async Task CancellationPropagatesEvenForEmptyRuleSet() =>
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Engine().MatchAsync(Input("file.pdf", []), new CancellationToken(true)));
}
