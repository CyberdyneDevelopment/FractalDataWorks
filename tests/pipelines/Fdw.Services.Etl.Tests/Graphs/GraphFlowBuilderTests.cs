using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for turning a stored graph into a flow graph and running it (FDW-1396): the stored settings reach the components, a
/// stored graph that cannot mean anything is refused with a message naming it, and a built graph runs on the engine.
/// </summary>
public sealed class GraphFlowBuilderTests
{
    private readonly FlowExecutor _executor = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStoredSourceAndMapRunOnTheEngineAndTheMapShapesEachRow()
    {
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(5), events);
        var graph = Graph(
            [DataSetSource("src"), Mapped("map", ("Key", "Id"))],
            [Link("src", "map")],
            Dropped("map"));

        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var tally = new FlowRunTally(_executor);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        tally.Extracted.ShouldBe(5);
        events.ShouldBe(["read@0", "read@2", "read@4"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStageComponentRunsAnExistingStageKindOnTheEngine()
    {
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(5), events);
        var graph = Graph(
            [DataSetSource("src"), FilterStage("keep", "Id >= 2")],
            [Link("src", "keep")],
            Dropped("keep"));

        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var tally = new FlowRunTally(_executor);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        tally.Extracted.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AToolComponentWithNoStageIsRefusedNamingTheSetting()
    {
        var empty = new PipelineComponentConfiguration { Name = "keep", ComponentType = "Filter" };
        var graph = Graph([DataSetSource("src"), empty], [Link("src", "keep")], Dropped("keep"));

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("keep");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphWithNoComponentsIsRefused()
    {
        var graph = Steps();

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("Sample");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoComponentsWithOneNameAreRefused()
    {
        var graph = Graph([DataSetSource("same"), Mapped("same", ("A", "B"))], []);

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("same");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AKindNothingKnowsIsRefusedAsNotRunnable()
    {
        var odd = new PipelineComponentConfiguration { Name = "odd", ComponentType = "Teleport" };
        var graph = Graph([odd], []);

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("cannot run yet");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ALoopThatStatesNoSettingsIsRefusedNamingItAndTheSetting()
    {
        var loop = new PipelineComponentConfiguration { Name = "loop", ComponentType = "ForLoop" };
        var graph = Graph([loop], []);

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("loop");
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("variable");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnInputMappingThatIsIncompleteIsRefusedNamingTheField()
    {
        var map = Mapped("map");
        map.Fields.Add(new PipelineComponentFieldConfiguration { SettingName = "mappings", InputName = "input", Ordinal = 0, TargetField = "Total" });
        var graph = Graph([DataSetSource("src"), map], [Link("src", "map")], Dropped("map"));

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("Total");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AJoinNamingAnUnknownJoinKindIsRefused()
    {
        var join = new PipelineComponentConfiguration
        {
            Name = "join", ComponentType = "MergeJoin", JoinKind = "Sideways", RightPrefix = "r_", BatchSize = 10,
        };
        join.Columns.Add(new PipelineComponentColumnConfiguration { SettingName = "leftKeys", Ordinal = 0, ColumnName = "Id" });
        join.Columns.Add(new PipelineComponentColumnConfiguration { SettingName = "rightKeys", Ordinal = 0, ColumnName = "Id" });
        var graph = Graph([join], []);

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("Sideways");
    }
}
