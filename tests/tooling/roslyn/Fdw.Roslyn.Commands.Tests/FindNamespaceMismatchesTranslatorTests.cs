using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Translators;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="FindNamespaceMismatchesTranslator"/> (FDW-595).
/// </summary>
public sealed class FindNamespaceMismatchesTranslatorTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TypeWhoseNamespaceMatchesPathAndProjectIsNotReported()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId)
            .AddType(projectId, "Fdw.Sample", "Widgets/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand(), solution, TestContext.Current.CancellationToken);

        // Zero matches is a fail-loud, not an empty success.
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task PathMismatchIsReportedWithPathKind()
    {
        // Namespace says Widgets, the file sits in Gizmos.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId)
            .AddType(projectId, "Fdw.Sample", "Gizmos/Gadget.cs", "Fdw.Sample.Widgets", "Gadget");

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand { IncludeTypes = true }, solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var report = result.Value.ShouldNotBeNull().Data;
        report.TotalMismatches.ShouldBe(1);
        report.Groups[0].MismatchKind.ShouldBe("Path");
        report.Groups[0].Types[0].ExpectedPath.ShouldNotBeNull().Replace('\\', '/')
            .ShouldEndWith("Fdw.Sample/Widgets/Gadget.cs");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ProjectMismatchIsReportedWithProjectKind()
    {
        // The verified real case: types namespaced Fdw.Data.MsSql living in the Connections project.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var connectionsId)
            .AddProject("Fdw.Data", out _)
            .AddType(connectionsId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand(), solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var report = result.Value.ShouldNotBeNull().Data;
        report.TotalMismatches.ShouldBe(1);
        report.Groups[0].MismatchKind.ShouldBe("Project");
        report.Groups[0].CurrentProject.ShouldBe("Fdw.Services.Connections.MsSql");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task NamespaceMatchingNoProjectIsReportedWithANoticeNotThrown()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var connectionsId)
            .AddType(connectionsId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand(), solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var group = result.Value.ShouldNotBeNull().Data.Groups[0];

        group.ExpectedProjectExists.ShouldBeFalse();
        group.ExpectedProject.ShouldBeNull();
        group.SuggestedAction.ShouldBe("CreateProject or MoveNamespace");
        group.Notice.ShouldNotBeNull();
        group.Notice!.ShouldContain("No project is named 'Fdw.Data.MsSql'");
        group.Notice.ShouldContain("MoveNamespace");
        result.Value.Data.GroupsWithoutTargetProject.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task TypeOptionIsFlaggedOnTheFinding()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var connectionsId)
            .AddType(connectionsId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType", isTypeOption: true);

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand { IncludeTypes = true }, solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var report = result.Value.ShouldNotBeNull().Data;
        report.TypeOptionCount.ShouldBe(1);
        report.Groups[0].Types[0].IsTypeOption.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task MismatchesSharingOneCauseCollapseIntoOneGroup()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var connectionsId);

        for (var i = 0; i < 12; i++)
        {
            solution = solution.AddType(
                connectionsId, "Fdw.Services.Connections.MsSql",
                $"NativeTypes/Type{i}.cs", "Fdw.Data.MsSql", $"Type{i}");
        }

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand(), solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var report = result.Value.ShouldNotBeNull().Data;

        report.TotalMismatches.ShouldBe(12);
        report.GroupCount.ShouldBe(1);
        report.Groups.Single().TypeCount.ShouldBe(12);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TypesAreOmittedByDefaultSoASolutionWideScanStaysReadable()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var connectionsId)
            .AddType(connectionsId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand(), solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var group = result.Value.ShouldNotBeNull().Data.Groups[0];

        group.TypeCount.ShouldBe(1);
        group.Types.ShouldBeEmpty();
        group.TypesOmitted.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task MaxTypesPerGroupCapsThePayloadAndCountsWhatItDropped()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var connectionsId);

        for (var i = 0; i < 10; i++)
        {
            solution = solution.AddType(
                connectionsId, "Fdw.Services.Connections.MsSql",
                $"NativeTypes/Type{i}.cs", "Fdw.Data.MsSql", $"Type{i}");
        }

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand { IncludeTypes = true, MaxTypesPerGroup = 4 },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var group = result.Value.ShouldNotBeNull().Data.Groups[0];

        group.TypeCount.ShouldBe(10);
        group.Types.Count.ShouldBe(4);
        group.TypesOmitted.ShouldBe(6);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task NearestAncestorProjectIsNotProposedAsTheDestination()
    {
        // Fdw.Data exists; Fdw.Data.MsSql does not. Folding SQL Server types into the generic Fdw.Data
        // package is a decision, not a default, so ExpectedProject stays null and the ancestor is
        // reported separately for information.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var connectionsId)
            .AddProject("Fdw.Data", out _)
            .AddType(connectionsId, "Fdw.Services.Connections.MsSql", "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand { IncludeTypes = true }, solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var group = result.Value.ShouldNotBeNull().Data.Groups[0];

        group.ExpectedProject.ShouldBeNull();
        group.ExpectedProjectExists.ShouldBeFalse();
        group.NearestOwningProject.ShouldBe("Fdw.Data");
        group.Types[0].ExpectedPath.ShouldBeNull();
        group.Notice.ShouldNotBeNull();
        group.Notice!.ShouldContain("Fdw.Data");
    }
}
