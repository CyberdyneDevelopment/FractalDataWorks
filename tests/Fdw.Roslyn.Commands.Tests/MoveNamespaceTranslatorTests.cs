using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="MoveNamespaceTranslator"/> (FDW-595).
/// </summary>
public sealed class MoveNamespaceTranslatorTests
{
    private static Solution WithTests(Solution solution) =>
        solution.AddProject("Fdw.Sample.Tests", out _);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task RewritesDeclarationUsingQualifiedNameAndCrefAcrossFiles()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);
        solution = WithTests(solution);

        solution = solution.AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");

        // A separate file that references the namespace three ways: using, qualified name, and a cref.
        solution = solution.AddRaw(projectId, "Fdw.Sample", "Consumer.cs", """
using Fdw.Sample.Widgets;

namespace Fdw.Sample.App;

/// <summary>See <see cref="Fdw.Sample.Widgets.Gadget"/>.</summary>
public class Consumer
{
    private Fdw.Sample.Widgets.Gadget? field;
}
""");

        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Sample.Widgets",
                NewNamespace = "Fdw.Data.Widgets",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveNamespaceData>>();

        mutation.Data.DocumentsChanged.ShouldBe(2);
        mutation.Data.ConsumerImpact.ShouldContain("CONSUMER-BREAKING");

        var consumer = mutation.NewSolution.Projects
            .SelectMany(p => p.Documents)
            .Single(d => d.Name == "Consumer.cs");
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldContain("using Fdw.Data.Widgets;");
        text.ShouldContain("cref=\"Fdw.Data.Widgets.Gadget\"");
        text.ShouldContain("private Fdw.Data.Widgets.Gadget? field;");
        text.ShouldNotContain("Fdw.Sample.Widgets");

        var symbolChange = mutation.SymbolChanges.Single();
        symbolChange.OldFullyQualifiedName.ShouldBe("Fdw.Sample.Widgets.Gadget");
        symbolChange.NewFullyQualifiedName.ShouldBe("Fdw.Data.Widgets.Gadget");
        symbolChange.ChangeType.ShouldBe(SymbolChangeTypes.Renamed.Name);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task FailsLoudWhenTestProjectsAreAbsentFromTheWorkspace()
    {
        // No *.Tests project loaded: a solution-wide rewrite would be incomplete by construction.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId)
            .AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");

        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Sample.Widgets",
                NewNamespace = "Fdw.Data.Widgets",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task DryRunReturnsAQueryResultSoItCannotReachTheMutationBranch()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);
        solution = WithTests(solution)
            .AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");

        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Sample.Widgets",
                NewNamespace = "Fdw.Data.Widgets",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var preview = result.Value.ShouldBeOfType<QueryResult<MoveNamespaceData>>();

        // The handler records the ledger only when IsMutation is true AND NewSolution is non-null.
        preview.IsMutation.ShouldBeFalse();
        preview.NewSolution.ShouldBeNull();
        preview.Data.WasDryRun.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ReportsTypeOptionIdsChangedBecauseIdIsDerivedFromTheFullyQualifiedName()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);
        solution = WithTests(solution)
            .AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget", isTypeOption: true);

        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Sample.Widgets",
                NewNamespace = "Fdw.Data.Widgets",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveNamespaceData>>().Data;

        data.TypeOptionIdsChanged.ShouldBe(1);
        data.ConsumerImpact.ShouldContain("FNV-1a Id");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task FailsLoudWhenTheSelectorMatchesNothing()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);
        solution = WithTests(solution)
            .AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");

        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Nothing.Here",
                NewNamespace = "Fdw.Data.Widgets",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
