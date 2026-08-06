using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Helpers;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests that every change previews what it breaks, attributes it per type, and can skip the offenders.
/// </summary>
public sealed class MoveVerificationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task MoveNamespacePreviewReportsReferencesItFailedToFollow()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId)
            .AddProject("Fdw.Sample.Tests", out _);

        solution = solution.AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");

        // A reference the blanket rewriter cannot follow: the namespace is reached via a using, and the
        // type is used unqualified, so renaming the declaration leaves this dangling.
        solution = solution.AddRaw(projectId, "Fdw.Sample", "Consumer.cs", """
namespace Fdw.Sample.App;

public class Consumer
{
    private Fdw.Sample.Widgets.Missing? gone;
}
""");

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

        // The preview must say what it breaks rather than rewriting silently.
        data.Breaks.ShouldNotBeEmpty();
        data.UnresolvedCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task MoveNamespaceRefusesToApplyAChangeThatWouldNotCompile()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId)
            .AddProject("Fdw.Sample.Tests", out _);

        solution = solution.AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");
        solution = solution.AddRaw(projectId, "Fdw.Sample", "Consumer.cs", """
namespace Fdw.Sample.App;

public class Consumer
{
    private Fdw.Sample.Widgets.Missing? gone;
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

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("ChangeWouldNotCompile");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task FindingsAreAttributedToTheTypeThatCausesThem()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var sourceId)
            .AddProject("Fdw.Data.MsSql", out var targetId);

        solution = solution.AddType(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/Clean.cs", "Fdw.Data.MsSql", "Clean");
        solution = solution.AddType(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/Dirty.cs", "Fdw.Data.MsSql", "Dirty");

        // Only Dirty collides.
        solution = solution.AddRaw(targetId, "Fdw.Data.MsSql", "Existing.cs", """
namespace Fdw.Data.MsSql;

public class Dirty
{
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>().Data;

        var collision = data.Breaks.First(b => b.Kind == "TypeCollision");
        collision.AffectedType.ShouldBe("Dirty");
        data.Breaks.ShouldNotContain(b => b.AffectedType == "Clean");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task SkippingTheOffendingTypeLetsTheRestMoveCleanly()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var sourceId)
            .AddProject("Fdw.Data.MsSql", out var targetId);

        solution = solution.AddType(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/Clean.cs", "Fdw.Data.MsSql", "Clean");
        solution = solution.AddType(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/Dirty.cs", "Fdw.Data.MsSql", "Dirty");
        solution = solution.AddRaw(targetId, "Fdw.Data.MsSql", "Existing.cs", """
namespace Fdw.Data.MsSql;

public class Dirty
{
}
""");

        // Address-or-skip: exclude the offender and the batch goes through.
        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand
            {
                Namespace = "Fdw.Data.MsSql",
                DryRun = false,
                SkipTypes = new[] { "Dirty" },
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypeToProjectData>>();

        mutation.Data.DocumentsMoved.ShouldBe(1);
        mutation.NewSolution.GetProject(targetId).ShouldNotBeNull()
            .Documents.ShouldContain(d => d.Name == "Clean.cs");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ACycleIsDetectedBeforeItIsCreated()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.A", out var a)
            .AddProject("Fdw.B", out var b)
            .AddProject("Fdw.C", out var c);

        // A -> B -> C already. Adding C -> A would close the loop.
        solution = solution.AddProjectReference(a, new ProjectReference(b));
        solution = solution.AddProjectReference(b, new ProjectReference(c));

        ProjectReferenceCycle.WouldCreateCycle(solution, c, a).ShouldBeTrue();
        ProjectReferenceCycle.WouldCreateCycle(solution, a, c).ShouldBeFalse();

        var described = ProjectReferenceCycle.DescribeCycle(solution, c, a).ShouldNotBeNull();
        described.ShouldContain("Fdw.C");
        described.ShouldContain("Fdw.A");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SelfReferenceCountsAsACycle()
    {
        var solution = NamespaceReconcileTestSolution.Empty().AddProject("Fdw.A", out var a);

        ProjectReferenceCycle.WouldCreateCycle(solution, a, a).ShouldBeTrue();
    }
}
