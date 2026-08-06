using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests for <see cref="MigrationGuideMarkdownFormatter"/>.
/// </summary>
public sealed class MigrationGuideMarkdownFormatterTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BuildWithEmptyEntriesReportsNoChangesRecorded()
    {
        var markdown = MigrationGuideMarkdownFormatter.Build("MySolution", Array.Empty<ChangeLedgerEntry>());

        markdown.ShouldContain("No changes were recorded in this session.");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BuildWithRenameSymbolChangeEmitsRenamesTableRow()
    {
        var rename = new SymbolChange("Old.Foo", "Old.Bar", SymbolChangeTypes.Renamed.Name, "Method", null, null, "Asm", "Asm", null);
        var entry = new ChangeLedgerEntry(
            1,
            "Rename",
            "Renamed Foo to Bar",
            Array.Empty<LedgerFileChange>(),
            new[] { rename },
            Array.Empty<PathChange>());

        var markdown = MigrationGuideMarkdownFormatter.Build("MySolution", new[] { entry });

        markdown.ShouldContain("## Renames");
        markdown.ShouldContain("| Old.Foo | Old.Bar | Method |");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BuildWithPathChangesEmitsMovesTableRow()
    {
        var pathChange = new PathChange("/src/A/A.csproj", "/src/B/A.csproj", "Project");
        var entry = new ChangeLedgerEntry(
            1,
            "MoveProjects",
            "Moved A",
            Array.Empty<LedgerFileChange>(),
            Array.Empty<SymbolChange>(),
            new[] { pathChange });

        var markdown = MigrationGuideMarkdownFormatter.Build("MySolution", new[] { entry });

        markdown.ShouldContain("## Moves");
        markdown.ShouldContain("| /src/A/A.csproj | /src/B/A.csproj | Project |");
    }
}
