using System.IO;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="WriteMigrationGuideTranslator"/>.
/// </summary>
public sealed class WriteMigrationGuideTranslatorTests
{
    private static Solution NewSolution() => new AdhocWorkspace().CurrentSolution;

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateReturnsLedgerNotAvailableWhenLedgerIsNull()
    {
        var translator = new WriteMigrationGuideTranslator();
        var command = new WriteMigrationGuideCommand { OutputPath = "/tmp/guide.md", Ledger = null };

        var result = await translator.Translate(command, NewSolution(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("LedgerNotAvailable");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateReturnsOutputPathRequiredWhenOutputPathIsMissing(string? outputPath)
    {
        var translator = new WriteMigrationGuideTranslator();
        var command = new WriteMigrationGuideCommand { OutputPath = outputPath!, Ledger = new ChangeLedger() };

        var result = await translator.Translate(command, NewSolution(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code.Name.ShouldBe("OutputPathRequired");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateWritesFileOnHappyPath()
    {
        var ledger = new ChangeLedger();
        ledger.Record(
            "Rename", "Renamed Foo to Bar", Array.Empty<FileChange>(), Array.Empty<SymbolChange>(), Array.Empty<PathChange>());

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var outputPath = Path.Combine(tempDir, "MIGRATION.md");
        var translator = new WriteMigrationGuideTranslator();
        var command = new WriteMigrationGuideCommand { OutputPath = outputPath, Ledger = ledger };

        try
        {
            var result = await translator.Translate(command, NewSolution(), TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            var data = result.Value.ShouldNotBeNull().Data;
            data.OutputPath.ShouldBe(outputPath);
            data.EntryCount.ShouldBe(1);
            File.Exists(outputPath).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
