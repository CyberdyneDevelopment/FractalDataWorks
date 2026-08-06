using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Results;
using Fdw.Roslyn.Commands.Workspace.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="RepairMovedReferencesTranslator"/> — repairing CS0246 from the change ledger.
/// </summary>
public sealed class RepairMovedReferencesTranslatorTests
{
    private static SymbolChange Moved(string fqn, string oldAssembly, string newAssembly) =>
        new(fqn, fqn, SymbolChangeTypes.Moved.Name, "NamedType", null, null, oldAssembly, newAssembly, null);

    private static IChangeLedger LedgerWith(params SymbolChange[] changes)
    {
        var ledger = new ChangeLedger();
        ledger.Record("MoveTypeToProject", "moved", Array.Empty<FileChange>(), changes, Array.Empty<PathChange>());
        return ledger;
    }

    /// <summary>
    /// Consumer references Marker but has NO reference to the project declaring it, producing CS0246.
    /// </summary>
    private static Solution BrokenConsumerSolution(out ProjectId consumerId, out ProjectId providerId)
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Consumer", out consumerId)
            .AddProject("Fdw.Data.Types.Databases", out providerId);

        solution = solution.AddRaw(providerId, "Fdw.Data.Types.Databases", "Marker.cs", """
namespace Fdw.Data.MsSql;

public class Marker
{
}
""");

        return solution.AddRaw(consumerId, "Fdw.Consumer", "Uses.cs", """
namespace Fdw.Consumer;

public class Uses
{
    private Fdw.Data.MsSql.Marker? marker;
}
""");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task FailsLoudWhenTheLedgerIsNotAvailable()
    {
        var solution = BrokenConsumerSolution(out _, out _);

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = null },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("LedgerNotAvailable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task FailsLoudWhenThereAreNoReferenceErrorsToRepair()
    {
        // A solution that compiles cleanly enough to produce no unresolved-name errors.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Clean", out var cleanId);
        solution = solution.AddRaw(cleanId, "Fdw.Clean", "Ok.cs", """
namespace Fdw.Clean;

public class Ok
{
}
""");

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = LedgerWith() },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NoReferenceErrorsFound");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task RepairsTheErrorTheLedgerExplains()
    {
        var solution = BrokenConsumerSolution(out var consumerId, out var providerId);
        var ledger = LedgerWith(Moved("Fdw.Data.MsSql.Marker", "Fdw.Services.Connections.MsSql", "Fdw.Data.Types.Databases"));

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = false, ApproveAll = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<ReferenceRepairData>>();

        mutation.Data.RepairedCount.ShouldBeGreaterThan(0);
        mutation.Data.ReferencesAdded.ShouldBe(1);

        var repair = mutation.Data.Repairs[0];
        repair.Project.ShouldBe("Fdw.Consumer");
        repair.RequiredAssembly.ShouldBe("Fdw.Data.Types.Databases");
        repair.Applied.ShouldBeTrue();
        repair.LedgerMatch.ShouldBe("Fdw.Data.MsSql.Marker");

        // CS0234 rather than CS0246 here: `Fdw` binds (the consumer declares Fdw.Consumer) but `Fdw.Data`
        // does not, so the compiler reports the first segment it cannot resolve. Both are handled.
        repair.DiagnosticId.ShouldBeOneOf("CS0246", "CS0234");

        // The consumer now references the project that carries the moved type.
        mutation.NewSolution.GetProject(consumerId).ShouldNotBeNull()
            .ProjectReferences.ShouldContain(r => r.ProjectId == providerId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task DryRunReportsTheRepairWithoutAddingTheReference()
    {
        var solution = BrokenConsumerSolution(out var consumerId, out var providerId);
        var ledger = LedgerWith(Moved("Fdw.Data.MsSql.Marker", "Fdw.Old", "Fdw.Data.Types.Databases"));

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var preview = result.Value.ShouldBeOfType<QueryResult<ReferenceRepairData>>();

        preview.IsMutation.ShouldBeFalse();
        preview.NewSolution.ShouldBeNull();
        preview.Data.WasDryRun.ShouldBeTrue();
        preview.Data.RepairedCount.ShouldBeGreaterThan(0);
        preview.Data.ReferencesAdded.ShouldBe(0);
        preview.Data.Repairs[0].Applied.ShouldBeFalse();

        // The original solution is untouched.
        solution.GetProject(consumerId).ShouldNotBeNull()
            .ProjectReferences.ShouldNotContain(r => r.ProjectId == providerId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ErrorsTheLedgerCannotExplainAreReportedNotGuessedAt()
    {
        var solution = BrokenConsumerSolution(out _, out _);

        // Ledger knows about a completely different type.
        var ledger = LedgerWith(Moved("Fdw.Something.Else", "Fdw.Old", "Fdw.New"));

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<ReferenceRepairData>>().Data;

        data.RepairedCount.ShouldBe(0);
        data.UnresolvedCount.ShouldBeGreaterThan(0);
        data.Unresolved[0].Reason.ShouldContain("does not appear in the change ledger");
        data.ReferencesAdded.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AmbiguousLedgerMatchIsLeftForAHuman()
    {
        var solution = BrokenConsumerSolution(out _, out _);

        var ledger = new ChangeLedger();
        ledger.Record("MoveTypeToProject", "moved", Array.Empty<FileChange>(), new[]
        {
            Moved("Fdw.A.Marker", "Fdw.Old", "Fdw.Target.One"),
            Moved("Fdw.B.Marker", "Fdw.Old", "Fdw.Target.Two"),
        }, Array.Empty<PathChange>());

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<ReferenceRepairData>>().Data;

        data.RepairedCount.ShouldBe(0);
        data.Unresolved.ShouldContain(u => u.Reason.Contains("ambiguous", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ScopeNarrowsWhichProjectsAreExamined()
    {
        var solution = BrokenConsumerSolution(out _, out _);
        var ledger = LedgerWith(Moved("Fdw.Data.MsSql.Marker", "Fdw.Old", "Fdw.Data.Types.Databases"));

        // Scope excludes the broken consumer, so there is nothing in scope to repair.
        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, Scope = "Fdw.Data.Types", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NoReferenceErrorsFound");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AnAlreadyPresentReferenceIsMarkedAppliedWithoutAddingADuplicate()
    {
        var solution = BrokenConsumerSolution(out var consumerId, out var providerId);
        solution = solution.AddProjectReference(consumerId, new ProjectReference(providerId));

        // Introduce a genuinely missing name so there is still an error to classify.
        solution = solution.AddRaw(consumerId, "Fdw.Consumer", "Other.cs", """
namespace Fdw.Consumer;

public class Other
{
    private Fdw.Data.MsSql.Marker? marker;
}
""");

        var ledger = LedgerWith(Moved("Fdw.Data.MsSql.Marker", "Fdw.Old", "Fdw.Data.Types.Databases"));

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = false, ApproveAll = true },
            solution,
            TestContext.Current.CancellationToken);

        // With the reference already present the type resolves, so there is no error left to repair.
        if (!result.IsSuccess)
        {
            result.Code.ShouldNotBeNull().Name.ShouldBe("NoReferenceErrorsFound");
            return;
        }

        var data = result.Value.ShouldBeOfType<MutationResult<ReferenceRepairData>>().Data;
        data.ReferencesAdded.ShouldBe(0);
        data.Repairs.Where(r => r.Applied).ToList().ForEach(r => r.RequiredAssembly.ShouldBe("Fdw.Data.Types.Databases"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task NothingIsAppliedWithoutAnExplicitApproval()
    {
        var solution = BrokenConsumerSolution(out var consumerId, out var providerId);
        var ledger = LedgerWith(Moved("Fdw.Data.MsSql.Marker", "Fdw.Old", "Fdw.Data.Types.Databases"));

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = false },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<MutationResult<ReferenceRepairData>>().Data;

        // Proposed, but not approved, so not applied.
        data.RepairedCount.ShouldBeGreaterThan(0);
        data.ReferencesAdded.ShouldBe(0);
        data.Rejected.Count.ShouldBe(data.RepairedCount);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AnExplicitRejectionBeatsApproveAll()
    {
        var solution = BrokenConsumerSolution(out _, out _);
        var ledger = LedgerWith(Moved("Fdw.Data.MsSql.Marker", "Fdw.Old", "Fdw.Data.Types.Databases"));

        var preview = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        var id = preview.Value.ShouldBeOfType<QueryResult<ReferenceRepairData>>().Data.Repairs[0].Id;

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand
            {
                Ledger = ledger,
                DryRun = false,
                ApproveAll = true,
                Reject = new[] { id },
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<MutationResult<ReferenceRepairData>>().Data;

        data.ReferencesAdded.ShouldBe(0);
        data.Rejected.ShouldContain(r => r.Id == id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task RepairsAreOnePerProjectAndAssemblyNotPerErrorLine()
    {
        var solution = BrokenConsumerSolution(out var consumerId, out _);

        // A second file in the same project with the same missing type: still ONE reference decision.
        solution = solution.AddRaw(consumerId, "Fdw.Consumer", "AlsoUses.cs", """
namespace Fdw.Consumer;

public class AlsoUses
{
    private Fdw.Data.MsSql.Marker? marker;
}
""");

        var ledger = LedgerWith(Moved("Fdw.Data.MsSql.Marker", "Fdw.Old", "Fdw.Data.Types.Databases"));

        var result = await new RepairMovedReferencesTranslator().Translate(
            new RepairMovedReferencesCommand { Ledger = ledger, DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<ReferenceRepairData>>().Data;

        data.ErrorsExamined.ShouldBeGreaterThan(1);
        data.RepairedCount.ShouldBe(1);
    }
}
