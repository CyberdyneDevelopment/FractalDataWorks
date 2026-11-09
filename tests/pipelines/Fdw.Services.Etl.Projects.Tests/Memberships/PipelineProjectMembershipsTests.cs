using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.TypeCollections;
using Fdw.Services.Etl.Projects.Memberships;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Tests.Memberships;

/// <summary>
/// FDW-4449: which projects run each pipeline, worked out from the orchestration nodes alone: a step's project is the first Project node above it, at any depth.
/// </summary>
public sealed class PipelineProjectMembershipsTests
{
    private static readonly Guid Sales = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid Budget = Guid.Parse("00000000-0000-0000-0000-0000000000b1");

    private static OrchestrationNodeImplementationConfiguration Node(string type, string name, Guid? parent, params Guid[] pipelines) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        NodeTypeId = OrchestrationNodeTypes.ByName(type).Id,
        ParentId = parent,
        PipelineMemberships = pipelines.Select((id, i) => new OrchestrationNodePipelineMembershipConfiguration { PipelineId = id, Name = id.ToString(), Ordinal = i }).ToList()
    };

    private static IReadOnlyDictionary<Guid, IReadOnlyList<string>> Of(params OrchestrationNodeImplementationConfiguration[] nodes)
        => PipelineProjectMemberships.Of(nodes, NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStepNamesTheProjectAboveItsStage()
    {
        var project = Node("Project", "Nightly", null);
        var stage = Node("Stage", "Load", project.Id);
        var step = Node("Step", "Sales", stage.Id, Sales);

        var projects = Of(project, stage, step);

        projects[Sales].ShouldBe(["Nightly"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void APipelineInTwoProjectsNamesBothSorted()
    {
        var zeta = Node("Project", "Zeta", null);
        var alpha = Node("Project", "Alpha", null);
        var zetaStep = Node("Step", "S1", Node("Stage", "G1", zeta.Id).Id, Sales);
        var alphaStage = Node("Stage", "G2", alpha.Id);
        var alphaStep = Node("Step", "S2", alphaStage.Id, Sales, Budget);

        var projects = Of(zeta, alpha, alphaStage, alphaStep, zetaStep);

        projects[Sales].ShouldBe(["Alpha", "Zeta"]);
        projects[Budget].ShouldBe(["Alpha"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void APipelineNoProjectRunsIsAbsent()
    {
        var project = Node("Project", "Nightly", null);

        Of(project).ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AStepWhoseParentChainLeavesTheVisibleNodesIsSkippedAndTheOthersAreStillRead()
    {
        var project = Node("Project", "Nightly", null);
        var stage = Node("Stage", "Load", project.Id);
        var good = Node("Step", "Sales", stage.Id, Sales);
        var orphan = Node("Step", "Lost", Guid.NewGuid(), Budget);

        var projects = Of(project, stage, good, orphan);

        projects.Keys.ShouldBe([Sales]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void AParentLinkThatLoopsEndsInsteadOfHanging()
    {
        var first = Node("Step", "A", null, Sales);
        var second = Node("Stage", "B", first.Id);
        first.ParentId = second.Id;

        Of(first, second).ShouldBeEmpty();
    }
}
