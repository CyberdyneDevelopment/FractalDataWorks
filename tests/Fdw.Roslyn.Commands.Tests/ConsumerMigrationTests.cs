using System;
using System.IO;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Helpers;
using Fdw.Roslyn.Commands.Workspace.Results;
using Fdw.Roslyn.Commands.Workspace.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests the CONSUMER half of migration: repairing from a published guide, with no session ledger.
/// </summary>
public sealed class ConsumerMigrationTests
{
    private static SymbolChange Moved(string fqn, string oldAsm, string newAsm, string? position = "X.cs") =>
        new(fqn, fqn, SymbolChangeTypes.Moved.Name, "NamedType", "/old.cs", "/new.cs", oldAsm, newAsm, position);

    private static ChangeLedgerEntry Entry(params SymbolChange[] changes) =>
        new(1, "MoveTypeToProject", "moved", Array.Empty<LedgerFileChange>(), changes, Array.Empty<PathChange>());

    private static string TempFile() =>
        Path.Combine(Path.GetTempPath(), "fdw595-guide-" + Guid.NewGuid().ToString("N") + ".md");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void GuideRoundTripsThroughTheEmitterAndReader()
    {
        // The emitter's table is a CONTRACT once the reader parses it. This test is what stops a
        // cosmetic tweak to the markdown silently breaking every consumer's migration.
        var markdown = MigrationGuideMarkdownFormatter.Build("MySolution", new[]
        {
            Entry(
                Moved("Fdw.Data.MsSql.BinaryType", "Fdw.Services.Connections.MsSql", "Fdw.Data.Types.Databases", "NativeTypes/BinaryType.cs"),
                Moved("Fdw.Data.MsSql.MsSqlVarcharConverter", "Fdw.Services.Connections.MsSql", "Fdw.Data.Types.Databases", null)),
        });

        var path = TempFile();
        try
        {
            File.WriteAllText(path, markdown);
            var moves = MigrationGuideReader.ReadAssemblyMoves(path);

            moves.Count.ShouldBe(2);

            var binary = moves[0];
            binary.OldFullyQualifiedName.ShouldBe("Fdw.Data.MsSql.BinaryType");
            binary.NewFullyQualifiedName.ShouldBe("Fdw.Data.MsSql.BinaryType");
            binary.ChangeType.ShouldBe(SymbolChangeTypes.Moved.Name);
            binary.OldAssembly.ShouldBe("Fdw.Services.Connections.MsSql");
            binary.NewAssembly.ShouldBe("Fdw.Data.Types.Databases");
            binary.RelativePosition.ShouldBe("NativeTypes/BinaryType.cs");
            binary.CrossesAssembly.ShouldBeTrue();

            // "(not recorded)" must come back as null, not as a literal string.
            moves[1].RelativePosition.ShouldBeNull();
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryAppendedSectionIsRead()
    {
        // An appended guide accumulates a section per commit; a consumer jumping several versions needs
        // all of them, not only the newest.
        var path = TempFile();
        try
        {
            File.WriteAllText(path, MigrationGuideMarkdownFormatter.Build("S", Array.Empty<ChangeLedgerEntry>()));
            File.AppendAllText(path, MigrationGuideMarkdownFormatter.BuildSection(
                "slice-1", new[] { Entry(Moved("Fdw.A.One", "Old", "New.One")) }, DateTimeOffset.UnixEpoch));
            File.AppendAllText(path, MigrationGuideMarkdownFormatter.BuildSection(
                "slice-2", new[] { Entry(Moved("Fdw.A.Two", "Old", "New.Two")) }, DateTimeOffset.UnixEpoch));

            var moves = MigrationGuideReader.ReadAssemblyMoves(path);

            moves.Count.ShouldBe(2);
            moves.ShouldContain(m => m.NewFullyQualifiedName == "Fdw.A.One" && m.NewAssembly == "New.One");
            moves.ShouldContain(m => m.NewFullyQualifiedName == "Fdw.A.Two" && m.NewAssembly == "New.Two");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RenameSectionsAreNotReadAsAssemblyMoves()
    {
        // A rename is a genuine consumer break, not something to auto-repair with a reference.
        var path = TempFile();
        try
        {
            File.WriteAllText(path, MigrationGuideMarkdownFormatter.Build("S", new[]
            {
                Entry(new SymbolChange("Fdw.Old.W", "Fdw.New.W", SymbolChangeTypes.Renamed.Name, "NamedType", null, null, "Asm", "Asm", null)),
            }));

            MigrationGuideReader.ReadAssemblyMoves(path).ShouldBeEmpty();
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    /// <summary>Consumer solution: uses a type whose package it does not reference.</summary>
    private static Solution ConsumerSolution(out ProjectId consumerId, out ProjectId providerId)
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Consumer", out consumerId)
            .AddProject("Fdw.Data.Types.Databases", out providerId);

        solution = solution.AddRaw(providerId, "Fdw.Data.Types.Databases", "BinaryType.cs", """
namespace Fdw.Data.MsSql;

public class BinaryType
{
}
""");

        return solution.AddRaw(consumerId, "Fdw.Consumer", "Uses.cs", """
namespace Fdw.Consumer;

public class Uses
{
    private Fdw.Data.MsSql.BinaryType? field;
}
""");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConsumerRepairsFromTheGuideWithNoLedgerAtAll()
    {
        var path = TempFile();
        try
        {
            File.WriteAllText(path, MigrationGuideMarkdownFormatter.Build("Producer", new[]
            {
                Entry(Moved("Fdw.Data.MsSql.BinaryType", "Fdw.Services.Connections.MsSql", "Fdw.Data.Types.Databases")),
            }));

            var solution = ConsumerSolution(out var consumerId, out var providerId);

            var result = await new RepairMovedReferencesTranslator().Translate(
                new RepairMovedReferencesCommand
                {
                    GuidePath = path,     // absolute; no Ledger supplied at all
                    Ledger = null,
                    DryRun = false,
                    ApproveAll = true,
                },
                solution,
                TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeTrue();
            var mutation = result.Value.ShouldBeOfType<MutationResult<ReferenceRepairData>>();

            mutation.Data.RepairedCount.ShouldBeGreaterThan(0);
            mutation.Data.ReferencesAdded.ShouldBe(1);
            mutation.Data.Repairs[0].RequiredAssembly.ShouldBe("Fdw.Data.Types.Databases");

            mutation.NewSolution.GetProject(consumerId).ShouldNotBeNull()
                .ProjectReferences.ShouldContain(r => r.ProjectId == providerId);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AMissingGuideFailsLoudRatherThanFallingBackToTheLedger()
    {
        var solution = ConsumerSolution(out _, out _);

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand
            {
                GuidePath = Path.Combine(Path.GetTempPath(), "definitely-not-here-" + Guid.NewGuid().ToString("N") + ".md"),
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("MigrationGuideNotUsable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AGuideWithNoAssemblyMovesFailsLoud()
    {
        var path = TempFile();
        try
        {
            File.WriteAllText(path, MigrationGuideMarkdownFormatter.Build("S", Array.Empty<ChangeLedgerEntry>()));

            var result = await new RepairMovedReferencesTranslator().Translate(
                new RepairMovedReferencesCommand { GuidePath = path, DryRun = true },
                ConsumerSolution(out _, out _),
                TestContext.Current.CancellationToken);

            result.IsSuccess.ShouldBeFalse();
            result.Code.ShouldNotBeNull().Name.ShouldBe("MigrationGuideNotUsable");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
