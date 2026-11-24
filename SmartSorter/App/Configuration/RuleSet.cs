using System.Text.Json;
using System.Text.Json.Serialization;
using SmartSort.App.Core;

namespace SmartSort.App.Configuration;

/// <summary>A JSON-configurable collection. Engines take a validated snapshot of these mutable settings.</summary>
public sealed class RuleSet
{
    [JsonRequired]
    public List<Rule> Rules { get; set; } = [];

    public static RuleSet FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var result = JsonSerializer.Deserialize<RuleSet>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException("Rules JSON must contain a rule set.");
        result.Validate();
        return result;
    }

    public void Validate() => CreateSnapshot();

    internal Rule[] CreateSnapshot()
    {
        if (Rules is null) throw new ArgumentException("Rules must be an array; use [] for no rules.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var snapshot = new List<Rule>();
        foreach (var rule in Rules)
        {
            if (rule is null || string.IsNullOrWhiteSpace(rule.Name) || rule.Name.Any(char.IsControl))
                throw new ArgumentException("Each rule needs a non-empty, printable name.");
            var name = rule.Name.Trim();
            if (!names.Add(name)) throw new ArgumentException($"Duplicate rule name: {name}.");
            var extensions = ValidateTerms(rule.AppliesToExtensions, name).Select(NormalizeExtension).ToArray();
            var fileNames = ValidateTerms(rule.FileNameContains, name);
            var content = ValidateTerms(rule.ContentContains, name);
            if (extensions.Length + fileNames.Length + content.Length == 0)
                throw new ArgumentException($"Rule '{name}' has no matching criteria.");
            snapshot.Add(new Rule
            {
                Name = name,
                TargetSubfolder = RelativeTarget.Normalize(rule.TargetSubfolder),
                Priority = rule.Priority,
                AppliesToExtensions = extensions,
                FileNameContains = fileNames,
                ContentContains = content
            });
        }
        return snapshot.OrderByDescending(r => r.Priority).ThenBy(r => r.Name, StringComparer.Ordinal).ToArray();
    }

    internal static string NormalizeExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        var normalized = extension.Trim();
        if (normalized.StartsWith('.')) normalized = normalized[1..];
        if (normalized.Length == 0 || !normalized.All(char.IsAsciiLetterOrDigit))
            throw new ArgumentException("Extensions must be simple suffixes such as .pdf or jpg.");
        return "." + normalized.ToLowerInvariant();
    }

    private static string[] ValidateTerms(string[] terms, string ruleName)
    {
        if (terms is null || terms.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"Rule '{ruleName}' contains null or blank matching criteria.");
        return terms.Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
