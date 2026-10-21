using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Graphs.Tools;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the stage-backed tools in the toolbox (FDW-1600): each is its stage kind placed in a graph, so its settings are the
/// stage kind's members, a tool that must hold every row declares a batch size, and the generic Stage component is gone.
/// </summary>
public sealed class StageToolComponentTypeTests
{
    private static readonly string[] Tools = ["Filter", "Calculate", "Sort", "Aggregate", "Distinct", "LatestBy", "Validate"];

    private readonly FlowExecutor _executor = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryToolListsItsStageKindsMembersAndNoOtherStageSetting()
    {
        foreach (var name in Tools)
        {
            var tool = ComponentTypes.ByName(name).ShouldBeOfType<StageToolComponentTypeBase>(name);
            var stage = TransformTypes.ByName(name).ShouldBeAssignableTo<TransformTypeBase>(name);

            tool.StageKind.ShouldBe(name);
            tool.MemberNames.ShouldBe([.. stage.Members.Select(member => member.Name)], name);
            tool.Settings.Select(setting => setting.Name).ShouldContain("errorOutput", name);
            tool.Settings.Any(setting => setting.Name == "batchSize").ShouldBe(stage.RequiresCompleteInput, name);
            tool.Inputs.Select(node => node.Name).ShouldBe(["input"], name);
            tool.Outputs.Select(node => node.Name).ShouldBe(["output", "error"], name);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheComputeToolDeclaresAnOptionalBatchSizeForATransformationThatWorksOnWholeTables()
    {
        var compute = ComponentTypes.ByName("Compute").ShouldBeOfType<StageToolComponentTypeBase>();

        compute.MemberNames.ShouldContain("compute");
        compute.Settings.Single(setting => setting.Name == "batchSize").IsRequired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void EveryKindStatesACategoryTheToolboxGroupsBy()
    {
        var expected = new Dictionary<string, string>
        {
            ["Filter"] = "Filter", ["Distinct"] = "Filter", ["LatestBy"] = "Filter", ["Sort"] = "Transform",
            ["Calculate"] = "Calculate", ["Aggregate"] = "Calculate", ["Compute"] = "Calculate", ["Validate"] = "Validate",
            ["Lookup"] = "Combine", ["MergeJoin"] = "Combine", ["UnionAll"] = "Combine", ["CrossApply"] = "Combine", ["ConditionalSplit"] = "Split",
        };

        foreach (var (name, category) in expected)
        {
            ComponentTypes.ByName(name).Category.ShouldBe(category, name);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void TheGenericStageComponentIsNotInTheCatalogue()
    {
        ReferenceEquals(ComponentTypes.ByName("Stage"), ComponentTypes.NotFound).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AToolThatHoldsEveryRowIsRefusedWithoutABatchSize()
    {
        var sort = new PipelineComponentConfiguration { Name = "order", ComponentType = "Sort" };
        sort.Stages.Add(new PipelineTransformConfiguration
        {
            Name = "order", OperationType = "Sort", IsEnabled = true,
            SortFields = [new PipelineTransformSortFieldConfiguration { FieldName = "Id", Direction = "Ascending", Ordinal = 1 }],
        });

        var check = ComponentSettings.Check(sort, NullLogger.Instance);

        check.IsFailure.ShouldBeTrue();
        check.CurrentMessage.ShouldNotBeNull().ShouldContain("batchSize");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AToolThatWorksABatchAtATimeDoesNotTakeABatchSize()
    {
        var filter = FilterStage("keep", "Id > 1");
        filter.BatchSize = 10;

        var check = ComponentSettings.Check(filter, NullLogger.Instance);

        check.IsFailure.ShouldBeTrue();
        check.CurrentMessage.ShouldNotBeNull().ShouldContain("batchSize");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AToolWhoseStageIsOfAnotherKindIsRefused()
    {
        var wrong = FilterStage("keep", "Id > 1");
        wrong.ComponentType = "Distinct";

        var built = new DistinctFlowComponentBuilder().Build(wrong, Context(Graph([DataSetSource("src"), wrong], [Link("src", "keep")]), new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("Filter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASortToolHoldsEveryRowAndSendsThemInOrder()
    {
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(5), events);
        var sort = new PipelineComponentConfiguration { Name = "order", ComponentType = "Sort", BatchSize = 2 };
        sort.Stages.Add(new PipelineTransformConfiguration
        {
            Name = "order", OperationType = "Sort", IsEnabled = true,
            SortFields = [new PipelineTransformSortFieldConfiguration { FieldName = "Id", Direction = "Descending", Ordinal = 1 }],
        });
        var graph = Graph([DataSetSource("src"), sort], [Link("src", "order")], Dropped("order"));

        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var tally = new FlowRunTally(_executor);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        tally.Extracted.ShouldBe(5);
    }
}
