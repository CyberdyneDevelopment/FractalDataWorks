using System;
using System.IO;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests that a relative migration-guide path resolves against the SOLUTION directory, so an in-repo
/// path is deterministic and can be tracked across commits.
/// </summary>
public sealed class RelativeGuidePathTests
{
    private static Solution SolutionAt(string solutionFilePath)
    {
        var workspace = new AdhocWorkspace();
        return workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: solutionFilePath));
    }

    private static ChangeLedger LedgerWithOneMove()
    {
        var ledger = new ChangeLedger();
        ledger.Record("MoveTypeToProject", "moved", Array.Empty<FileChange>(), new[]
        {
            new SymbolChange("Fdw.A.W", "Fdw.A.W", SymbolChangeTypes.Moved.Name, "NamedType",
                "/old.cs", "/new.cs", "Fdw.Old", "Fdw.New", "W.cs"),
        }, Array.Empty<PathChange>());
        return ledger;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task RelativePathResolvesAgainstTheSolutionDirectoryNotTheProcessDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "fdw595-rel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var solution = SolutionAt(Path.Combine(root, "Repo.slnx"));

            var result = await new WriteMigrationGuideTranslator().Translate(
                new WriteMigrationGuideCommand
                {
                    OutputPath = "PACKAGE-MIGRATION.md",
                    Overwrite = false,
                    SectionTitle = "slice-1",
                    Ledger = LedgerWithOneMove(),
                },
                solution,
                TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();

            var expected = Path.Combine(root, "PACKAGE-MIGRATION.md");
            File.Exists(expected).ShouldBeTrue();
            (await File.ReadAllTextAsync(expected, TestContext.Current.CancellationToken))
                .ShouldContain("# slice-1 —");

            // And NOT next to the running process.
            File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "PACKAGE-MIGRATION.md")).ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task NestedRelativePathIsHonoured()
    {
        var root = Path.Combine(Path.GetTempPath(), "fdw595-rel-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var solution = SolutionAt(Path.Combine(root, "Repo.slnx"));

            var result = await new WriteMigrationGuideTranslator().Translate(
                new WriteMigrationGuideCommand
                {
                    OutputPath = Path.Combine("docs", "ledger.md"),
                    Ledger = LedgerWithOneMove(),
                },
                solution,
                TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            File.Exists(Path.Combine(root, "docs", "ledger.md")).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task RelativePathFailsLoudWhenTheSolutionHasNoFilePath()
    {
        // An in-memory solution gives nothing to resolve against — say so rather than writing somewhere
        // arbitrary relative to the server process.
        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()));

        var result = await new WriteMigrationGuideTranslator().Translate(
            new WriteMigrationGuideCommand { OutputPath = "PACKAGE-MIGRATION.md", Ledger = LedgerWithOneMove() },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("RelativeOutputPathNeedsSolutionPath");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AbsolutePathIsUsedUnchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), "fdw595-abs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var target = Path.Combine(root, "elsewhere", "guide.md");
            var solution = SolutionAt(Path.Combine(root, "Repo.slnx"));

            var result = await new WriteMigrationGuideTranslator().Translate(
                new WriteMigrationGuideCommand { OutputPath = target, Ledger = LedgerWithOneMove() },
                solution,
                TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            File.Exists(target).ShouldBeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
