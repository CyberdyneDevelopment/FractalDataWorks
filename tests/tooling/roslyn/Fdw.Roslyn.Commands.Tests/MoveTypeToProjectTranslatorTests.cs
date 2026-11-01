using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="MoveTypeToProjectTranslator"/> (FDW-595).
/// </summary>
public sealed class MoveTypeToProjectTranslatorTests
{
    private static Solution TwoProjectSolution(out ProjectId sourceId, out ProjectId targetId)
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out sourceId)
            .AddProject("Fdw.Data.MsSql", out targetId);
        return solution;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task MovesDocumentAcrossProjectsAndAddsTheTargetReference()
    {
        var solution = TwoProjectSolution(out var sourceId, out var targetId)
            .AddProject("Fdw.Collections", out var collectionsId);

        solution = solution.AddType(
            collectionsId, "Fdw.Collections", "Marker.cs", "Fdw.Collections", "Marker");

        // The source must reference Fdw.Collections for Marker to bind — a document can only use an
        // assembly its project references, and RequiredReferences is derived from bound symbols.
        solution = solution.AddProjectReference(sourceId, new ProjectReference(collectionsId));

        solution = solution.AddRaw(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", """
using Fdw.Collections;

namespace Fdw.Data.MsSql;

public class BinaryType
{
    private Marker? marker;
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = false },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypeToProjectData>>();

        mutation.Data.SourceProject.ShouldBe("Fdw.Services.Connections.MsSql");
        mutation.Data.TargetProject.ShouldBe("Fdw.Data.MsSql");
        mutation.Data.DocumentsMoved.ShouldBe(1);

        // The document now belongs to the target project, at the path its namespace implies.
        var target = mutation.NewSolution.GetProject(targetId).ShouldNotBeNull();
        var moved = target.Documents.Single(d => d.Name == "BinaryType.cs");
        moved.FilePath.ShouldNotBeNull().Replace('\\', '/').ShouldEndWith("Fdw.Data.MsSql/BinaryType.cs");

        mutation.NewSolution.GetProject(sourceId).ShouldNotBeNull()
            .Documents.ShouldNotContain(d => d.Name == "BinaryType.cs");

        // References fixed on the target side: it now references what the moved document needs.
        target.ProjectReferences.ShouldContain(r => r.ProjectId == collectionsId);

        // The FQN is unchanged, so this is a Moved change, not a Renamed one.
        var symbolChange = mutation.SymbolChanges.Single();
        symbolChange.ChangeType.ShouldBe(SymbolChangeTypes.Moved.Name);
        symbolChange.OldFullyQualifiedName.ShouldBe(symbolChange.NewFullyQualifiedName);
        symbolChange.CrossesAssembly.ShouldBeTrue();
        symbolChange.OldAssembly.ShouldBe("Fdw.Services.Connections.MsSql");
        symbolChange.NewAssembly.ShouldBe("Fdw.Data.MsSql");
        symbolChange.RelativePosition.ShouldBe("BinaryType.cs");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task RequiredReferencesAreDerivedFromTheSymbolGraph()
    {
        var solution = TwoProjectSolution(out var sourceId, out _)
            .AddProject("Fdw.Collections", out var collectionsId);

        solution = solution.AddType(collectionsId, "Fdw.Collections", "Marker.cs", "Fdw.Collections", "Marker");
        solution = solution.AddProjectReference(sourceId, new ProjectReference(collectionsId));

        solution = solution.AddRaw(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", """
using Fdw.Collections;

namespace Fdw.Data.MsSql;

public class BinaryType
{
    private Marker? marker;
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>().Data;

        data.RequiredReferences.ShouldContain(r => r.Assembly == "Fdw.Collections");
        data.RequiredReferences.First(r => r.Assembly == "Fdw.Collections").BecauseOf.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task DroppableReferencesReportsZeroHonestlyWhenNothingBecomesDroppable()
    {
        // The source keeps a document that still needs Fdw.Collections, so nothing becomes droppable.
        var solution = TwoProjectSolution(out var sourceId, out _)
            .AddProject("Fdw.Collections", out var collectionsId);

        solution = solution.AddType(collectionsId, "Fdw.Collections", "Marker.cs", "Fdw.Collections", "Marker");
        solution = solution.AddProjectReference(sourceId, new ProjectReference(collectionsId));

        solution = solution.AddRaw(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", """
using Fdw.Collections;

namespace Fdw.Data.MsSql;

public class BinaryType
{
    private Marker? marker;
}
""");

        solution = solution.AddRaw(sourceId, "Fdw.Services.Connections.MsSql", "Stays.cs", """
using Fdw.Collections;

namespace Fdw.Services.Connections.MsSql;

public class Stays
{
    private Marker? marker;
}
""");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>().Data;

        // Guard: prove the scanner actually bound the symbol, so an empty droppable list means
        // "nothing became droppable" rather than "nothing resolved".
        data.RequiredReferences.ShouldContain(r => r.Assembly == "Fdw.Collections");
        data.DroppableReferences.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TypeOptionIsReportedInBreaksOnCrossProjectMove()
    {
        var solution = TwoProjectSolution(out var sourceId, out _);

        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs",
            "Fdw.Data.MsSql", "BinaryType", isTypeOption: true);

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var data = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>().Data;

        data.Breaks.ShouldContain(b => b.Kind == "TypeOptionRegistrationMoves");
        data.Breaks.First(b => b.Kind == "TypeOptionRegistrationMoves").Detail
            .ShouldContain("clean build does not prove this");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task FailsLoudWithAlternativesWhenTheTargetProjectDoesNotExist()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var sourceId)
            .AddType(sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task DryRunReturnsAQueryResultSoItCannotReachTheMutationBranch()
    {
        var solution = TwoProjectSolution(out var sourceId, out _);
        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var preview = result.Value.ShouldBeOfType<QueryResult<MoveTypeToProjectData>>();

        preview.IsMutation.ShouldBeFalse();
        preview.NewSolution.ShouldBeNull();
        preview.Data.WasDryRun.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task NothingReachesDiskUntilApplyWorkspaceChanges()
    {
        var solution = TwoProjectSolution(out var sourceId, out var targetId);
        solution = solution.AddType(
            sourceId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        var result = await new MoveTypeToProjectTranslator().Translate(
            new MoveTypeToProjectCommand { Namespace = "Fdw.Data.MsSql", DryRun = false },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypeToProjectData>>();

        // The mutation exists only as a new Solution. The translator never writes; persistence is
        // ApplyWorkspaceChanges' job, driven by the handler.
        var newPath = mutation.NewSolution.GetProject(targetId)!.Documents.Single().FilePath.ShouldNotBeNull();
        File.Exists(newPath).ShouldBeFalse();
        Directory.Exists(Path.GetDirectoryName(newPath)).ShouldBeFalse();
    }
}
