using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for a test run of a graph (FDW-1665): sources read only the first rows, destinations write nothing, and steps with an
/// outside effect do nothing, while the flow runs as it does for real.
/// </summary>
public sealed class GraphTestRunTests
{
    private static readonly GraphTestRun Test = new(5, true);

    private static PipelineComponentConfiguration Sink() => new()
    {
        Name = "keep", ComponentType = "DataSetDestination", DataSetName = "Out", LoadKind = "BulkInsert", TruncateFirst = true,
    };

    private static async Task<(FlowRunTally Tally, List<string> Events)> Run(GraphPipelineConfiguration graph, int rows, GraphTestRun? test)
    {
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(rows), events);
        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway, null, test));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var tally = new FlowRunTally(new FlowExecutor());
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);
        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        return (tally, events);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASourceReadsOnlyTheCapAndAsksForNoMoreThanTheCapLeaves()
    {
        var graph = Graph([DataSetSource("src", batchSize: 2), Sink()], [Link("src", "keep")]);

        var (tally, events) = await Run(graph, 100, Test);

        tally.Extracted.ShouldBe(5);
        events.Where(e => e.StartsWith("read@", System.StringComparison.Ordinal)).ShouldBe(["read@0", "read@2", "read@4"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADestinationInATestRunWritesNothingAndTruncatesNothing()
    {
        var graph = Graph([DataSetSource("src"), Sink()], [Link("src", "keep")]);

        var (tally, events) = await Run(graph, 20, Test);

        tally.Transformed.ShouldBe(5);
        events.Where(e => e.StartsWith("load", System.StringComparison.Ordinal) || e == "truncate").ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ASourceThatCannotPageIsCutToTheCap()
    {
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(50), events, ignoresPaging: true);
        var graph = Graph([DataSetSource("src", batchSize: 2), Sink()], [Link("src", "keep")]);
        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway, null, Test));
        var tally = new FlowRunTally(new FlowExecutor());

        var ran = await built.Value!.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        tally.Extracted.ShouldBe(5);
    }

    private static PipelineComponentConfiguration SqlSource(int maxRows) => new()
    {
        Name = "src", ComponentType = "SqlSource", DataStoreName = "Warehouse", ScriptText = "SELECT Id FROM dbo.T", MaxRows = maxRows, BatchSize = 2,
    };

    private static async Task<(FlowRunTally Tally, Fdw.Results.IGenericResult Ran)> RunScript(int maxRows, int selected, GraphTestRun test)
    {
        var rows = PagedSourceGateway.Rows(selected).Select(row => (IDictionary<string, object?>)row).ToList();
        var graph = Graph([SqlSource(maxRows), Sink()], [Link("src", "keep")]);
        var built = GraphFlowBuilder.Build(graph, Context(graph, new ScriptingGateway(rows), test));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var tally = new FlowRunTally(new FlowExecutor());
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);
        return (tally, ran);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AScriptSourceKeepsTheFirstRowsUpToTheCapInATestRun()
    {
        var (tally, ran) = await RunScript(maxRows: 100, selected: 50, new GraphTestRun(3, true));

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        tally.Extracted.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AScriptThatSelectsMoreThanItsLimitIsStillRefusedWhenTheCapIsAboveTheLimit()
    {
        var (_, ran) = await RunScript(maxRows: 4, selected: 9, new GraphTestRun(100, true));

        ran.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARealRunReadsEveryRow()
    {
        var graph = Graph([DataSetSource("src", batchSize: 4), FilterStage("keep", "Id >= 0")], [Link("src", "keep")], Dropped("keep"));

        var (tally, _) = await Run(graph, 12, null);

        tally.Extracted.ShouldBe(12);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AStepWithAnOutsideEffectIsBuiltAsOneThatDoesNothingInATestRunOnly()
    {
        FlowStepBuilders.ByName("Notify").ShouldBeAssignableTo<FlowStepBuilderBase>().HasOutsideEffect.ShouldBeTrue();
        FlowStepBuilders.ByName("ExecuteSql").ShouldBeAssignableTo<FlowStepBuilderBase>().HasOutsideEffect.ShouldBeTrue();
        FlowStepBuilders.ByName("ExecutePipeline").ShouldBeAssignableTo<FlowStepBuilderBase>().HasOutsideEffect.ShouldBeTrue();
        FlowStepBuilders.ByName("SetVariable").ShouldBeAssignableTo<FlowStepBuilderBase>().HasOutsideEffect.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANotifyStepInATestRunSendsNothingAndTheRunStillSucceeds()
    {
        var graph = Steps(
            DataFlow("flow", [DataSetSource("src"), Sink()], [Link("src", "keep")], [], 0),
            Notify("tell", "OnCall", "AfterLoad", "done", 1));
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(6), events);

        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway, null, Test));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
    }
}
