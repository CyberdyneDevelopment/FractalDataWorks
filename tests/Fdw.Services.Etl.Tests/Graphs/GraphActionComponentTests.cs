using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for running the actions and loops of a stored graph (FDW-1403, FDW-2041): a loop runs the components in its body, a variable set in a body ends a
/// do-while, and a variable or a value that cannot be read is refused with a message naming it.
/// </summary>
public sealed class GraphActionComponentTests
{
    private static async Task<(FlowRunTally Tally, FlowVariables Variables, Fdw.Results.IGenericResult Ran)> Run(
        GraphPipelineConfiguration graph, List<string> events)
    {
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(5), events);
        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var variables = GraphVariables.Declare(graph, null, NullLogger.Instance);
        variables.IsSuccess.ShouldBeTrue(variables.CurrentMessage);
        var tally = new FlowRunTally(new FlowExecutor());
        var ran = await built.Value!.Run(StepContext(variables.Value!, tally), TestContext.Current.CancellationToken);
        return (tally, variables.Value!, ran);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ALoopRunsTheDataFlowInItsBodyOnceForEachCounterValue()
    {
        var events = new List<string>();
        var graph = Steps(ForLoop("loop", "I", 1, 3), SourceFlow("body", parent: "loop"));

        var (tally, _, ran) = await Run(graph, events);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        tally.Extracted.ShouldBe(15);
        events.Count.ShouldBe(9);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADoWhileEndsWhenTheComponentsInItsBodyMakeItsConditionFalse()
    {
        var events = new List<string>();
        var graph = Steps(
            DoWhile("again", "Attempt < 3", 4),
            SourceFlow("body", ordinal: 0, parent: "again"),
            SetVariable("advance", "Attempt", "Int32", "5", ordinal: 1, parent: "again"));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Attempt", DataType = "Int32", ValueText = "0" });

        var (tally, variables, ran) = await Run(graph, events);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        tally.Extracted.ShouldBe(5);
        variables.TryGet("Attempt", out var attempt).ShouldBeTrue();
        attempt.ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ADoWhileThatNeverEndsFailsAtItsStatedMostNotQuietly()
    {
        var graph = Steps(DoWhile("again", "Attempt < 3", 2), SourceFlow("body", parent: "again"));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Attempt", DataType = "Int32", ValueText = "0" });

        var (_, _, ran) = await Run(graph, []);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("again");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ASetVariableOfAVariableNobodyDeclaredFails()
    {
        var graph = Steps(SetVariable("set", "Missing", "Int32", "1"));

        var (_, _, ran) = await Run(graph, []);

        ran.IsFailure.ShouldBeTrue();
        ran.CurrentMessage.ShouldNotBeNull().ShouldContain("Missing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASetVariableWhoseValueItsTypeCannotReadIsRefusedNamingTheType()
    {
        var graph = Steps(SetVariable("set", "N", "Int32", "many"));

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("Int32");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AVariableWhoseStartingValueItsTypeCannotReadIsRefused()
    {
        var graph = Steps(SourceFlow("flow"));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "N", DataType = "Int32", ValueText = "many" });

        var declared = GraphVariables.Declare(graph, null, NullLogger.Instance);

        declared.IsFailure.ShouldBeTrue();
        declared.CurrentMessage.ShouldNotBeNull().ShouldContain("N");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoComponentsWithOneNameAreRefused()
    {
        var graph = Steps(SourceFlow("same"), SourceFlow("same", ordinal: 1));

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("same");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnExecutePipelineComponentIsBuiltWithItsParametersMadeFromVariables()
    {
        var start = new PipelineComponentConfiguration
        {
            Name = "start", ComponentType = "ExecutePipeline", TargetPipeline = "Child", AwaitCompletion = true, FailOnChildFailure = true,
        };
        start.Parameters.Add(new PipelineComponentParameterConfiguration { ParameterName = "Year", ValueText = "{{Season}}" });
        var graph = Steps(start);

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
    }
}
