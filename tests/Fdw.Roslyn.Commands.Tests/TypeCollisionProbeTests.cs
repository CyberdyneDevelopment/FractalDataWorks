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
/// Tests that a move is validated by the COMPILER, not by name or path matching.
/// </summary>
public sealed class TypeCollisionProbeTests
{
    private static Solution TwoProjects(out ProjectId sourceId, out ProjectId targetId) =>
        NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out sourceId)
            .AddProject("Fdw.Data.MsSql", out targetId);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ACleanMoveReportsNoCollision()
    {
        var solution = TwoProjects(out var sourceId, out _);
        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs",
            "Fdw.Data.MsSql", "BinaryType");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>().Data;

        data.Breaks.ShouldNotContain(b => b.Kind == "TypeCollision");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ADuplicateDefinitionInTheTargetIsCaughtEvenThoughTheFilenameDiffers()
    {
        // The target already declares the type, but in a DIFFERENTLY-NAMED file — invisible to a path
        // check, obvious to the compiler (CS0101).
        var solution = TwoProjects(out var sourceId, out var targetId);
        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs",
            "Fdw.Data.MsSql", "BinaryType");
        solution = solution.AddRaw(targetId, "Fdw.Data.MsSql", "SomethingElse.cs", """
namespace Fdw.Data.MsSql;

public class BinaryType
{
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>().Data;

        var collision = data.Breaks.FirstOrDefault(b => b.Kind == "TypeCollision");
        collision.ShouldNotBeNull();
        collision!.Detail.ShouldContain("CS0101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ANamespaceSegmentThatIsAlsoATypeNameIsCaught()
    {
        // The exact shape that bit this codebase: a namespace segment shadowed by a type of the same
        // name, so the qualified name stops binding. No name or path comparison finds this.
        var solution = TwoProjects(out var sourceId, out var targetId);
        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs",
            "Fdw.Data.MsSql", "BinaryType");

        // A type named "MsSql" sitting in Fdw.Data — the parent of the Fdw.Data.MsSql namespace.
        solution = solution.AddRaw(targetId, "Fdw.Data.MsSql", "Shadow.cs", """
namespace Fdw.Data;

public class MsSql
{
}

public class Consumer
{
    private Fdw.Data.MsSql.BinaryType? field;
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>().Data;

        var collision = data.Breaks.FirstOrDefault(b => b.Kind == "TypeCollision");
        collision.ShouldNotBeNull();
        // Which id fires depends on how the compiler resolves the shadowed segment — CS0426 (name not
        // found inside a type), CS0118 (used as the wrong kind) or CS0234. Asserting one exactly would be
        // brittle; what matters is that the probe classifies it as a collision at all, which no name or
        // path comparison would have done.
        collision!.Detail.ShouldContain("CS");
        new[] { "CS0426", "CS0118", "CS0234", "CS0101" }
            .Any(id => collision.Detail.Contains(id, StringComparison.Ordinal))
            .ShouldBeTrue($"expected a collision diagnostic, got: {collision.Detail}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ARealRunRefusesToApplyAKnownCollision()
    {
        var solution = TwoProjects(out var sourceId, out var targetId);
        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs",
            "Fdw.Data.MsSql", "BinaryType");
        solution = solution.AddRaw(targetId, "Fdw.Data.MsSql", "SomethingElse.cs", """
namespace Fdw.Data.MsSql;

public class BinaryType
{
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = false },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("MoveWouldCollide");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task APreviewStillReportsCollisionsRatherThanFailing()
    {
        var solution = TwoProjects(out var sourceId, out var targetId);
        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs",
            "Fdw.Data.MsSql", "BinaryType");
        solution = solution.AddRaw(targetId, "Fdw.Data.MsSql", "Dup.cs", """
namespace Fdw.Data.MsSql;

public class BinaryType
{
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        // Previewing exists precisely to surface this before anyone commits to it.
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>()
            .Data.Breaks.ShouldContain(b => b.Kind == "TypeCollision");
    }
}
