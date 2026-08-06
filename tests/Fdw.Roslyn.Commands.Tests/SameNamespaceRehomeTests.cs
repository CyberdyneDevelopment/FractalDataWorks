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
/// The real re-home shape: the types that USE an interface sit in the same namespace as it, so they
/// reference it with no using at all.
/// </summary>
/// <remarks>
/// The original guard only looked for an explicit `using &lt;old&gt;;`, which such a file never has —
/// so nothing was added and ReferencesFollowed reported 0. These tests pin the implicit-scope cases.
/// </remarks>
public sealed class SameNamespaceRehomeTests
{
    private static string PathOf(string project, string relative) =>
        System.IO.Path.Combine(NamespaceReconcileTestSolution.Root, "src", project, relative);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConsumerInTheSameNamespaceGainsAUsing()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Mcp.Bus", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Mcp.Bus", "IBus.cs", "Fdw.Mcp.Bus", "IBus");

        // Same namespace as IBus, so it references it with NO using.
        solution = solution.AddRaw(projectId, "Fdw.Mcp.Bus", "BusImpl.cs", """
namespace Fdw.Mcp.Bus;

public class BusImpl
{
    private IBus? bus;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus", "IBus.cs") },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        mutation.Data.ReferencesFollowed.ShouldBeGreaterThan(0);

        var impl = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "BusImpl.cs");
        var text = (await impl.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldContain("using Fdw.Mcp.Bus.Abstractions;");
        text.ShouldContain("private IBus? bus;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AConsumerInADescendantNamespaceAlsoGainsAUsing()
    {
        // C# lookup walks enclosing scopes, so Fdw.Mcp.Bus.Internal sees Fdw.Mcp.Bus without a using.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Mcp.Bus", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Mcp.Bus", "IBus.cs", "Fdw.Mcp.Bus", "IBus");
        solution = solution.AddRaw(projectId, "Fdw.Mcp.Bus", "Internal/Helper.cs", """
namespace Fdw.Mcp.Bus.Internal;

public class Helper
{
    private IBus? bus;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus", "IBus.cs") },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var helper = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Helper.cs");
        var text = (await helper.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldContain("using Fdw.Mcp.Bus.Abstractions;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task NoUsingIsAddedWhenTheNewNamespaceIsAlreadyInScope()
    {
        // Moving INTO an ancestor of the consumer's own namespace: already visible, so a using here
        // would be unused — an error in this build.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Mcp.Bus", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Mcp.Bus", "Deep/IBus.cs", "Fdw.Mcp.Bus.Deep", "IBus");
        solution = solution.AddRaw(projectId, "Fdw.Mcp.Bus", "Deep/User.cs", """
namespace Fdw.Mcp.Bus.Deep;

public class User
{
    private IBus? bus;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus", "Deep/IBus.cs") },
                NewNamespace = "Fdw.Mcp.Bus",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var user = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "User.cs");
        var text = (await user.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldNotContain("using Fdw.Mcp.Bus;");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ASimilarlyPrefixedNamespaceIsNotTreatedAsNested()
    {
        // Fdw.Mcp.BusOther must NOT count as nested inside Fdw.Mcp.Bus.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Mcp.Bus", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Mcp.Bus", "IBus.cs", "Fdw.Mcp.Bus", "IBus");
        solution = solution.AddRaw(projectId, "Fdw.Mcp.Bus", "Other.cs", """
namespace Fdw.Mcp.BusOther;

public class Other
{
    private int unrelated;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { PathOf("Fdw.Mcp.Bus", "IBus.cs") },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var other = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Other.cs");
        var text = (await other.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldNotContain("using Fdw.Mcp.Bus.Abstractions;");
    }
}
