using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Roslyn.Commands;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests that the migration guide is readable by a CONSUMER, in their repo, not just by its author.
/// </summary>
public sealed class MigrationGuideConsumerFormatTests
{
    private static ChangeLedgerEntry Move(string fqn, string oldAsm, string newAsm) =>
        new(1, "MoveTypeToProject", "summary", Array.Empty<LedgerFileChange>(),
            new[]
            {
                new SymbolChange(
                    fqn, fqn, SymbolChangeTypes.Moved.Name, "NamedType",
                    "/home/someone/.worktrees/scratch/src/A/Thing.cs",
                    "/home/someone/.worktrees/scratch/src/B/Thing.cs",
                    oldAsm, newAsm, "Thing.cs"),
            },
            Array.Empty<PathChange>());

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheMovesTableNamesTypesAndPackagesNotMachinePaths()
    {
        var markdown = MigrationGuideMarkdownFormatter.Build("S", new[]
        {
            Move("Fdw.Data.MsSql.VarCharType", "Fdw.Services.Connections.MsSql", "Fdw.Data.MsSql"),
        });

        // What a consumer can act on: this type is now in that package.
        markdown.ShouldContain("Fdw.Data.MsSql.VarCharType");
        markdown.ShouldContain("Fdw.Data.MsSql");

        // What is useless in their repo.
        markdown.ShouldNotContain("/home/someone/", Case.Sensitive);
        markdown.ShouldNotContain(".worktrees", Case.Sensitive);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void TheDocumentHeaderIsAHeaderNotAnEmptyReport()
    {
        // The append path prepends this when the guide does not exist yet. Build() with zero entries
        // produced a whole "0 change(s) recorded / No changes were recorded" report, which then sat above
        // the real section in every guide it created.
        var header = MigrationGuideMarkdownFormatter.BuildHeader("MySolution");

        header.ShouldContain("MySolution");
        header.ShouldNotContain("0 change(s) recorded", Case.Sensitive);
        header.ShouldNotContain("No changes were recorded", Case.Sensitive);
    }
}
