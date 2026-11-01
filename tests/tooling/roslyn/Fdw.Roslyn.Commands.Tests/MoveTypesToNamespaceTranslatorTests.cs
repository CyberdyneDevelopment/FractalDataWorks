using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="MoveTypesToNamespaceTranslator"/> — re-homing SPECIFIC types without disturbing
/// the types that legitimately share their namespace.
/// </summary>
public sealed class MoveTypesToNamespaceTranslatorTests
{
    private const string Root = NamespaceReconcileTestSolution.Root;

    private static string PathOf(string project, string relative) =>
        System.IO.Path.Combine(Root, "src", project, relative);

    /// <summary>
    /// The exact shape MoveNamespace cannot express: 2 files in one project wrongly declare a namespace
    /// that 3 files in another project declare correctly.
    /// </summary>
    private static Solution SplitNamespaceSolution(out ProjectId wrongId, out ProjectId rightId)
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Mcp.Bus.Abstractions", out wrongId)
            .AddProject("Fdw.Mcp.Bus", out rightId);

        // Wrongly declaring Fdw.Mcp.Bus while sitting in the Abstractions project.
        solution = solution.AddType(wrongId, "Fdw.Mcp.Bus.Abstractions", "IBusOne.cs", "Fdw.Mcp.Bus", "IBusOne");
        solution = solution.AddType(wrongId, "Fdw.Mcp.Bus.Abstractions", "IBusTwo.cs", "Fdw.Mcp.Bus", "IBusTwo");

        // Correctly declaring Fdw.Mcp.Bus in the implementation project — must NOT be touched.
        solution = solution.AddType(rightId, "Fdw.Mcp.Bus", "BusOne.cs", "Fdw.Mcp.Bus", "BusOne");
        solution = solution.AddType(rightId, "Fdw.Mcp.Bus", "BusTwo.cs", "Fdw.Mcp.Bus", "BusTwo");
        solution = solution.AddType(rightId, "Fdw.Mcp.Bus", "BusThree.cs", "Fdw.Mcp.Bus", "BusThree");

        return solution;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task OnlyTheSelectedFilesAreRehomedAndSiblingsAreLeftAlone()
    {
        var solution = SplitNamespaceSolution(out var wrongId, out var rightId);

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[]
                {
                    PathOf("Fdw.Mcp.Bus.Abstractions", "IBusOne.cs"),
                    PathOf("Fdw.Mcp.Bus.Abstractions", "IBusTwo.cs"),
                },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        mutation.Data.DeclarationsChanged.ShouldBe(2);
        mutation.Data.TypesLeftBehind.ShouldBe(3);

        // The two selected files now declare the new namespace...
        foreach (var name in new[] { "IBusOne.cs", "IBusTwo.cs" })
        {
            var doc = mutation.NewSolution.GetProject(wrongId).ShouldNotBeNull().Documents.Single(d => d.Name == name);
            var text = (await doc.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
            text.ShouldContain("namespace Fdw.Mcp.Bus.Abstractions;");
        }

        // ...and the three correct files are untouched. This is what MoveNamespace could not do.
        foreach (var name in new[] { "BusOne.cs", "BusTwo.cs", "BusThree.cs" })
        {
            var doc = mutation.NewSolution.GetProject(rightId).ShouldNotBeNull().Documents.Single(d => d.Name == name);
            var text = (await doc.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
            text.ShouldContain("namespace Fdw.Mcp.Bus;");
            text.ShouldNotContain("Fdw.Mcp.Bus.Abstractions");
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AQualifiedReferenceToAMovedTypeIsFollowedWhileASiblingIsNot()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Sample", "Moved.cs", "Fdw.Sample.Old", "Moved");
        solution = solution.AddType(projectId, "Fdw.Sample", "Stays.cs", "Fdw.Sample.Old", "Stays");

        solution = solution.AddRaw(projectId, "Fdw.Sample", "Consumer.cs", """
namespace Fdw.Sample.App;

public class Consumer
{
    private Fdw.Sample.Old.Moved? moved;
    private Fdw.Sample.Old.Stays? stays;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Sample", "Moved.cs") },
                NewNamespace = "Fdw.Sample.New",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var consumer = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Consumer.cs");
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        // The moved type's reference follows; the sibling's does not.
        text.ShouldContain("Fdw.Sample.New.Moved? moved;");
        text.ShouldContain("Fdw.Sample.Old.Stays? stays;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AnUnqualifiedReferenceGainsAUsingForTheNewNamespace()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Sample", "Moved.cs", "Fdw.Sample.Old", "Moved");
        // A sibling stays behind, so `using Fdw.Sample.Old;` remains valid after the move.
        solution = solution.AddType(projectId, "Fdw.Sample", "Stays.cs", "Fdw.Sample.Old", "Stays");

        solution = solution.AddRaw(projectId, "Fdw.Sample", "Consumer.cs", """
using Fdw.Sample.Old;

namespace Fdw.Sample.App;

public class Consumer
{
    private Moved? moved;
    private Stays? stays;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Sample", "Moved.cs") },
                NewNamespace = "Fdw.Sample.New",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var consumer = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Consumer.cs");
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldContain("using Fdw.Sample.New;");
        text.ShouldContain("private Moved? moved;");

        // The sibling that stayed keeps resolving through the original using.
        text.ShouldContain("using Fdw.Sample.Old;");
        text.ShouldContain("private Stays? stays;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheRenameIsRecordedAsConsumerBreakingWithTheFqnChange()
    {
        var solution = SplitNamespaceSolution(out _, out _);

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus.Abstractions", "IBusOne.cs") },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var change = mutation.SymbolChanges.Single();
        change.ChangeType.ShouldBe(SymbolChangeTypes.Renamed.Name);
        change.OldFullyQualifiedName.ShouldBe("Fdw.Mcp.Bus.IBusOne");
        change.NewFullyQualifiedName.ShouldBe("Fdw.Mcp.Bus.Abstractions.IBusOne");
        mutation.Data.ConsumerImpact.ShouldContain("CONSUMER-BREAKING");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task SkipTypesExcludesAnOffenderSoTheRestStillMoves()
    {
        var solution = SplitNamespaceSolution(out _, out _);

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[]
                {
                    PathOf("Fdw.Mcp.Bus.Abstractions", "IBusOne.cs"),
                    PathOf("Fdw.Mcp.Bus.Abstractions", "IBusTwo.cs"),
                },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                SkipTypes = new[] { "IBusTwo" },
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>().Data;

        data.DeclarationsChanged.ShouldBe(1);
        data.MovedTypes.ShouldHaveSingleItem().ShouldContain("IBusOne");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task DryRunReturnsAQueryResultAndChangesNothing()
    {
        var solution = SplitNamespaceSolution(out var wrongId, out _);

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus.Abstractions", "IBusOne.cs") },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var preview = result.Value.ShouldBeOfType<QueryResult<MoveTypesToNamespaceData>>();

        preview.IsMutation.ShouldBeFalse();
        preview.NewSolution.ShouldBeNull();
        preview.Data.WasDryRun.ShouldBeTrue();
        preview.Data.DeclarationsChanged.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task SelectingATypeAlreadyInTheTargetNamespaceFailsLoud()
    {
        var solution = SplitNamespaceSolution(out _, out _);

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus", "BusOne.cs") },
                NewNamespace = "Fdw.Mcp.Bus",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("TargetSameAsCurrent");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AFileThatMatchesNothingFailsLoud()
    {
        var solution = SplitNamespaceSolution(out _, out _);

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus", "DoesNotExist.cs") },
                NewNamespace = "Fdw.Somewhere",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NoTypesMatchedSelector");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task EmptyingANamespaceRepairsTheNowDeadImport()
    {
        // Re-homing the LAST type out of a namespace makes every `using` of it dangle. This used to be
        // REPORTED — preview counted it as unresolved, a real run refused with ChangeWouldNotCompile —
        // which made the command unusable for its main purpose: it created the break itself, by adding
        // the new import and leaving the old one pointing at a namespace it had just emptied. Reporting a
        // break you caused and could fix is not verification, it is a stalemate. Now it repairs it.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Sample", "Only.cs", "Fdw.Sample.Old", "Only");
        solution = solution.AddRaw(projectId, "Fdw.Sample", "Consumer.cs", """
using Fdw.Sample.Old;

namespace Fdw.Sample.App;

public class Consumer
{
    private Only? only;
}
""");

        var preview = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Sample", "Only.cs") },
                NewNamespace = "Fdw.Sample.New",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        preview.IsSuccess.ShouldBeTrue();
        preview.Value.ShouldBeOfType<QueryResult<MoveTypesToNamespaceData>>()
            .Data.UnresolvedCount.ShouldBe(0, "the dangling import is repaired, not reported");

        // And a real run now completes, because there is nothing left to refuse over.
        var applied = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Sample", "Only.cs") },
                NewNamespace = "Fdw.Sample.New",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        applied.IsSuccess.ShouldBeTrue();
        var mutation = applied.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var consumer = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => string.Equals(d.Name, "Consumer.cs", StringComparison.Ordinal));
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldNotContain("using Fdw.Sample.Old;", Case.Sensitive);
        text.ShouldContain("using Fdw.Sample.New;", Case.Sensitive);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ANamespaceThatStillHasTypesKeepsItsImport()
    {
        // The removal must be narrow. If a sibling stays behind the namespace still exists, the consumer
        // may still need the import, and deleting it would break code the move never touched.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Sample", "Widget.cs", "Fdw.Sample.Old", "Widget");
        solution = solution.AddType(projectId, "Fdw.Sample", "Gadget.cs", "Fdw.Sample.Old", "Gadget");
        solution = solution.AddRaw(projectId, "Fdw.Sample", "Both.cs", """
using Fdw.Sample.Old;

namespace Fdw.Sample.App;

public class Both
{
    private Widget? widget;
    private Gadget? gadget;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Sample", "Widget.cs") },
                NewNamespace = "Fdw.Sample.Moved",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var consumer = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => string.Equals(d.Name, "Both.cs", StringComparison.Ordinal));
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        // Gadget still lives there, so the old import is still load-bearing and must survive.
        text.ShouldContain("using Fdw.Sample.Old;", Case.Sensitive);
        text.ShouldContain("using Fdw.Sample.Moved;", Case.Sensitive);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task APreExistingErrorElsewhereDoesNotBlockTheMove()
    {
        // The probe used to report the affected projects' ABSOLUTE errors. Any solution that already had
        // a break — most real ones mid-refactor — therefore had every move refused with
        // ChangeWouldNotCompile for damage the move did not do. The verification is now a diff against a
        // baseline taken from the original solution, so only errors the change INTRODUCES count.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Sample", "Widget.cs", "Fdw.Sample.Old", "Widget");

        // Broken before anyone touched it, and nothing to do with the move.
        solution = solution.AddRaw(projectId, "Fdw.Sample", "AlreadyBroken.cs", """
namespace Fdw.Sample.Unrelated;

public class AlreadyBroken
{
    private ThisTypeDoesNotExist? field;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Sample", "Widget.cs") },
                NewNamespace = "Fdw.Sample.Moved",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue("the pre-existing break is not this move's doing");
        result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>()
            .Data.UnresolvedCount.ShouldBe(0, "only newly-introduced errors are reported");
    }
}
