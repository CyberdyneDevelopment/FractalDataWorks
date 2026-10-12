using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Workspace.Roslyn;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Translators;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;
using Moq;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Verifies which FDW-595 commands reach the change ledger (FDW-594's mutation branch) and which do not.
/// </summary>
/// <remarks>
/// The ledger is what a consumer reads after CS0246, so a preview appearing there as completed work would
/// actively mislead. These tests pin that behaviour through the real handler, not by inspection.
/// </remarks>
public sealed class NamespaceReconcileLedgerTests
{
    private static Mock<IRoslynWorkspace> NewWorkspace(Solution solution)
    {
        var mock = new Mock<IRoslynWorkspace>();
        mock.SetupGet(w => w.CurrentSolution).Returns(solution);
        mock.SetupGet(w => w.BaselineSolution).Returns(solution);
        return mock;
    }

    private static Mock<ITranslatorRegistry> NewRegistry(IRoslynCommandTranslator translator)
    {
        var mock = new Mock<ITranslatorRegistry>();
        mock.Setup(r => r.GetTranslator(It.IsAny<System.Type>()))
            .Returns(GenericResult<IRoslynCommandTranslator>.Success(translator));
        return mock;
    }

    private static Solution MoveableSolution()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var sourceId)
            .AddProject("Fdw.Data.MsSql", out _)
            .AddProject("Fdw.Sample.Tests", out _);

        return solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs",
            "Fdw.Data.MsSql", "BinaryType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ReadOnlyFindDoesNotEnterTheLedger()
    {
        var solution = MoveableSolution();
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(
            NewWorkspace(solution).Object,
            NewRegistry(new FindNamespaceMismatchesTranslator()).Object,
            ledger);

        var result = await handler.Execute(
            new FindNamespaceMismatchesCommand(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        ledger.Entries.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task DryRunMoveDoesNotEnterTheLedger()
    {
        var solution = MoveableSolution();
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(
            NewWorkspace(solution).Object,
            NewRegistry(new MoveTypeToProjectTranslator()).Object,
            ledger);

        var result = await handler.Execute(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        ledger.Entries.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CommittedMoveTypeToProjectEntersTheLedgerAsAMove()
    {
        var solution = MoveableSolution();
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(
            NewWorkspace(solution).Object,
            NewRegistry(new MoveTypeToProjectTranslator()).Object,
            ledger);

        var result = await handler.Execute(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = false },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var entry = ledger.Entries.ShouldHaveSingleItem();

        entry.CommandName.ShouldBe("MoveTypeToProject");
        var symbolChange = entry.SymbolChanges.ShouldHaveSingleItem();

        // A move is NOT a consumer break: the FQN is preserved and the assembly hop is recorded.
        symbolChange.ChangeType.ShouldBe(SymbolChangeTypes.Moved.Name);
        symbolChange.OldFullyQualifiedName.ShouldBe(symbolChange.NewFullyQualifiedName);
        symbolChange.CrossesAssembly.ShouldBeTrue();
        symbolChange.NewAssembly.ShouldBe("Fdw.Data.MsSql");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task CommittedMoveNamespaceEntersTheLedgerAsARename()
    {
        var solution = MoveableSolution();
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(
            NewWorkspace(solution).Object,
            NewRegistry(new MoveNamespaceTranslator()).Object,
            ledger);

        var result = await handler.Execute(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Data.MsSql",
                NewNamespace = "Fdw.Data.Types.Databases",
                DryRun = false,
            },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var entry = ledger.Entries.ShouldHaveSingleItem();

        entry.CommandName.ShouldBe("MoveNamespace");
        var symbolChange = entry.SymbolChanges.ShouldHaveSingleItem();

        // A rename IS a consumer break, and is recorded distinguishably from a move.
        symbolChange.ChangeType.ShouldBe(SymbolChangeTypes.Renamed.Name);
        symbolChange.OldFullyQualifiedName.ShouldNotBe(symbolChange.NewFullyQualifiedName);
        symbolChange.NewFullyQualifiedName.ShouldBe("Fdw.Data.Types.Databases.BinaryType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ReasonIsRecordedIntoTheLedgerSoTheGuideCanSayWhy()
    {
        var solution = MoveableSolution();
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(
            NewWorkspace(solution).Object,
            NewRegistry(new MoveTypeToProjectTranslator()).Object,
            ledger);

        var result = await handler.Execute(
            new MoveTypeToProjectCommand
            {
                Namespace = "Fdw.Data.MsSql",
                DryRun = false,
                Reason = "slice-1-vocabulary (FDW-602)",
            },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        ledger.Entries.ShouldHaveSingleItem().Summary.ShouldContain("reason: slice-1-vocabulary (FDW-602)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task NoReasonLeavesTheSummaryUnchanged()
    {
        var solution = MoveableSolution();
        var ledger = new ChangeLedger();
        var handler = new RoslynCommandHandler(
            NewWorkspace(solution).Object,
            NewRegistry(new MoveTypeToProjectTranslator()).Object,
            ledger);

        var result = await handler.Execute(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = false },
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        ledger.Entries.ShouldHaveSingleItem().Summary.ShouldNotContain("reason:");
    }
}
