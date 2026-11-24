using System.Text.Json;
using SmartSort.App.Configuration;
using SmartSort.App.Core;
using static SmartSort.Tests.RuleEngineTests;

namespace SmartSort.Tests;

public class RuleConfigurationTests
{
    [Fact]
    public void LoadsActualRulesArrayAndAcceptsEmptyArray()
    {
        var rules = RuleSet.FromJson("""{"Rules":[{"Name":"PDF","TargetSubfolder":"PDFs","AppliesToExtensions":[".pdf"]}]}""");
        Assert.Single(rules.Rules);
        Assert.Empty(RuleSet.FromJson("""{"Rules":[]}""").Rules);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"Rules\":{}}")]
    [InlineData("{\"Rulse\":[]}")]
    [InlineData("{\"Rules\":[{\"Unknown\":true}]}")]
    [InlineData("{")]
    public void RejectsMalformedOrMisspelledJson(string json) => Assert.Throws<JsonException>(() => RuleSet.FromJson(json));

    [Theory]
    [InlineData("{\"Rules\":null}")]
    [InlineData("{\"Rules\":[null]}")]
    [InlineData("{\"Rules\":[{}]}")]
    public void RejectsInvalidRuleCollections(string json) => Assert.Throws<ArgumentException>(() => RuleSet.FromJson(json));

    [Fact]
    public void RejectsDuplicateNamesBlankCriteriaAndImplicitCatchAll()
    {
        Assert.Throws<ArgumentException>(() => Engine(Rule("Same"), Rule("same")));
        var rule = Rule();
        rule.ContentContains = [" "];
        Assert.Throws<ArgumentException>(() => Engine(rule));
        rule.ContentContains = null!;
        Assert.Throws<ArgumentException>(() => Engine(rule));
        rule.ContentContains = [];
        rule.AppliesToExtensions = [];
        rule.FileNameContains = [];
        Assert.Throws<ArgumentException>(() => Engine(rule));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("..\\outside")]
    [InlineData("/absolute")]
    [InlineData("C:\\absolute")]
    [InlineData("safe/../../escape")]
    [InlineData("safe//empty")]
    [InlineData("safe/CON.txt")]
    [InlineData("safe/name.")]
    [InlineData("safe/name:stream")]
    [InlineData("safe/ name")]
    public void RejectsUnsafeTargetsBeforeProcessing(string target)
    {
        var rule = Rule();
        rule.TargetSubfolder = target;
        Assert.Throws<ArgumentException>(() => Engine(rule));
    }

    [Fact]
    public void NormalizesPortableNestedTargets() => Assert.Equal("Invoices/2026", RelativeTarget.Normalize("Invoices\\2026"));
}
