using System.IO;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="ChangeLedger.WriteMarkdown"/>.
/// </summary>
public sealed class ChangeLedgerWriteMarkdownTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task WriteMarkdownWithValidPathWritesFileAndReturnsSuccess()
    {
        var ledger = new ChangeLedger();
        ledger.Record(
            "Rename", "Renamed Foo to Bar", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var outputPath = Path.Combine(tempDir, "MIGRATION.md");

        try
        {
            var result = await ledger.WriteMarkdown(outputPath, "MySolution", TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            var guide = result.Value.ShouldNotBeNull();
            guide.OutputPath.ShouldBe(outputPath);
            guide.EntryCount.ShouldBe(1);
            File.Exists(outputPath).ShouldBeTrue();

            var content = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
            content.ShouldContain("Rename");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task WriteMarkdownWithUnwritablePathReturnsFailure()
    {
        var ledger = new ChangeLedger();

        // An existing regular file used as a directory component makes Directory.CreateDirectory throw.
        var blockingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        await File.WriteAllTextAsync(blockingFile, "not a directory", TestContext.Current.CancellationToken);
        var outputPath = Path.Combine(blockingFile, "MIGRATION.md");

        try
        {
            var result = await ledger.WriteMarkdown(outputPath, "MySolution", TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
        }
        finally
        {
            File.Delete(blockingFile);
        }
    }
}
