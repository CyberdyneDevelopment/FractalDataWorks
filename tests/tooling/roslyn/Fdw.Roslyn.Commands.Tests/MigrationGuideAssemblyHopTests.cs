using System;
using System.IO;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for the FDW-595 migration-guide additions: the assembly-move table, the consumer-impact
/// statements, and appending a guide that accumulates across commits.
/// </summary>
public sealed class MigrationGuideAssemblyHopTests
{
    private static ChangeLedgerEntry Entry(string commandName, params SymbolChange[] changes) =>
        new(1, commandName, "summary", Array.Empty<LedgerFileChange>(), changes, Array.Empty<PathChange>());

    private static SymbolChange CrossAssemblyMove(string fqn, string oldAsm, string newAsm, string? position) =>
        new(fqn, fqn, SymbolChangeTypes.Moved.Name, "NamedType",
            "/old/" + fqn + ".cs", "/new/" + fqn + ".cs", oldAsm, newAsm, position);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void CrossAssemblyMoveEmitsTheTypeToPackageTable()
    {
        var markdown = MigrationGuideMarkdownFormatter.Build("MySolution", new[]
        {
            Entry("MoveTypeToProject", CrossAssemblyMove(
                "Fdw.Data.MsSql.BinaryType", "Fdw.Services.Connections.MsSql",
                "Fdw.Data.Types.Databases", "NativeTypes/BinaryType.cs")),
        });

        markdown.ShouldContain("### Assembly moves (type -> new package)");
        markdown.ShouldContain(
            "| Fdw.Data.MsSql.BinaryType | Fdw.Services.Connections.MsSql | Fdw.Data.Types.Databases | NativeTypes/BinaryType.cs |");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void MovesAndRenamesCarryOppositeConsumerImpactStatements()
    {
        var markdown = MigrationGuideMarkdownFormatter.Build("MySolution", new[]
        {
            Entry("MoveNamespace", new SymbolChange(
                "Fdw.Old.Widget", "Fdw.New.Widget", SymbolChangeTypes.Renamed.Name, "NamedType", null, null, "Asm", "Asm", null)),
            Entry("MoveTypeToProject", CrossAssemblyMove(
                "Fdw.Data.MsSql.BinaryType", "Fdw.Old.Asm", "Fdw.New.Asm", "BinaryType.cs")),
        });

        markdown.ShouldContain("**These are consumer-breaking**");
        markdown.ShouldContain("FNV-1a");
        markdown.ShouldContain("**These are NOT consumer-breaking**");
        markdown.ShouldContain("CS0246");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void MissingRelativePositionIsStatedNotBlank()
    {
        var markdown = MigrationGuideMarkdownFormatter.Build("S", new[]
        {
            Entry("MoveTypeToProject", CrossAssemblyMove("Fdw.A.Widget", "Fdw.Old", "Fdw.New", null)),
        });

        markdown.ShouldContain("(not recorded)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void SameAssemblyMoveDoesNotClaimAPackageMove()
    {
        var markdown = MigrationGuideMarkdownFormatter.Build("S", new[]
        {
            Entry("MoveToFile", new SymbolChange(
                "Fdw.Same.Widget", "Fdw.Same.Widget", SymbolChangeTypes.Moved.Name, "NamedType",
                "/old.cs", "/new.cs", "Fdw.Same", "Fdw.Same", "Widget.cs")),
        });

        markdown.ShouldContain("## Moves");
        markdown.ShouldNotContain("### Assembly moves");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void BuildSectionIsTitledTimestampedAndSeparated()
    {
        var stamp = new DateTimeOffset(2026, 7, 28, 16, 30, 0, TimeSpan.Zero);

        var section = MigrationGuideMarkdownFormatter.BuildSection(
            "slice-1-vocabulary",
            new[] { Entry("MoveTypeToProject", CrossAssemblyMove("Fdw.A.W", "Fdw.Old", "Fdw.New", "W.cs")) },
            stamp);

        section.ShouldContain("---");
        section.ShouldContain("# slice-1-vocabulary — 2026-07-28 16:30:00");
        section.ShouldContain("### Assembly moves (type -> new package)");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AnExistingGuideIsAppendedToNotReplaced()
    {
        var path = Path.Combine(Path.GetTempPath(), "fdw595-guide-" + Guid.NewGuid().ToString("N") + ".md");
        try
        {
            var ledger = new ChangeLedger();
            ledger.Record("MoveTypeToProject", "first move", Array.Empty<FileChange>(),
                new[] { CrossAssemblyMove("Fdw.A.First", "Fdw.Old", "Fdw.New", "First.cs") },
                Array.Empty<PathChange>());

            (await ledger.WriteMarkdown(path, "S", overwrite: false, "slice-1", TestContext.Current.CancellationToken))
                .IsSuccess.ShouldBeTrue();

            var afterFirst = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            afterFirst.ShouldContain("# Migration Guide");   // header written on first append
            afterFirst.ShouldContain("# slice-1 —");
            afterFirst.ShouldContain("Fdw.A.First");

            // A second session appends rather than clobbering the first.
            var second = new ChangeLedger();
            second.Record("MoveTypeToProject", "second move", Array.Empty<FileChange>(),
                new[] { CrossAssemblyMove("Fdw.A.Second", "Fdw.Old", "Fdw.New", "Second.cs") },
                Array.Empty<PathChange>());

            (await second.WriteMarkdown(path, "S", overwrite: false, "slice-2", TestContext.Current.CancellationToken))
                .IsSuccess.ShouldBeTrue();

            var afterSecond = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            afterSecond.ShouldContain("# slice-1 —");
            afterSecond.ShouldContain("Fdw.A.First");
            afterSecond.ShouldContain("# slice-2 —");
            afterSecond.ShouldContain("Fdw.A.Second");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task OverwriteReplacesTheFileWhenExplicitlyAsked()
    {
        var path = Path.Combine(Path.GetTempPath(), "fdw595-guide-" + Guid.NewGuid().ToString("N") + ".md");
        try
        {
            await File.WriteAllTextAsync(path, "STALE CONTENT", TestContext.Current.CancellationToken);

            var ledger = new ChangeLedger();
            ledger.Record("MoveTypeToProject", "move", Array.Empty<FileChange>(),
                new[] { CrossAssemblyMove("Fdw.A.W", "Fdw.Old", "Fdw.New", "W.cs") },
                Array.Empty<PathChange>());

            (await ledger.WriteMarkdown(path, "S", overwrite: true, null, TestContext.Current.CancellationToken))
                .IsSuccess.ShouldBeTrue();

            var content = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            content.ShouldNotContain("STALE CONTENT");
            content.ShouldContain("# Migration Guide");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
