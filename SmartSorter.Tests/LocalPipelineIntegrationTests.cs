using System.IO.Abstractions;
using Serilog;
using SmartSort.App.Configuration;
using SmartSort.App.Core;
using SmartSort.App.Core.Extraction;
using SmartSort.App.Infrastructure;
using SmartSort.App.Processing;

namespace SmartSort.Tests;

public class LocalPipelineIntegrationTests
{
    [Fact]
    public async Task RealFilesystemBatchPreservesBytesAcrossDryRunAndNestedMoves()
    {
        var root = Path.Combine(Path.GetTempPath(), "smartsort-integration-" + Guid.NewGuid().ToString("N"));
        var input = Path.Combine(root, "in");
        var output = Path.Combine(root, "out");
        Directory.CreateDirectory(Path.Combine(input, "nested"));
        try
        {
            var expected = new Dictionary<string, string>();
            for (var index = 0; index < 20; index++)
            {
                var name = $"document-{index}.txt";
                var content = $"Exact bytes {index}: äöü\r\nnext line";
                expected[name] = content;
                File.WriteAllText(Path.Combine(index % 2 == 0 ? input : Path.Combine(input, "nested"), name), content);
            }
            var fs = new FileSystem();
            using var logger = new LoggerConfiguration().CreateLogger();
            var classifier = new FileClassifier(new RuleEngine(new RuleSet(), new CompositeTextExtractor([])));
            var processor = new DocumentProcessor(new FileSystemDocumentSource(fs, 1), classifier, new FileMover(fs), fs, output);
            var app = new SortApplication(new FileScanner(fs, logger), processor, logger, fs);
            var settings = new AppSettings { InputFolder = input, DefaultTarget = output, LogFolder = Path.Combine(root, "logs") };
            Assert.Equal(new ProcessingSummary(20, 0, 20, 0), await app.RunAsync(settings));
            Assert.False(Directory.Exists(output));
            settings.DryRun = false;
            Assert.Equal(new ProcessingSummary(20, 20, 0, 0), await app.RunAsync(settings));
            Assert.Empty(Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories));
            foreach (var (name, content) in expected)
                Assert.Equal(content, File.ReadAllText(Path.Combine(output, "_Uncategorized", name)));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
