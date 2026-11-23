using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Flow;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// FDW-6139: the graph builder makes a step whose kind states an outside effect claim itself before it acts, in one place for every such kind; a test run builds it as a step that does nothing, so it claims
/// nothing, and a step that has no outside effect is not claimed.
/// </summary>
public sealed class GraphStepClaimsTests
{
    private static Mock<IRuleNotifier> Notifier(List<RuleNotification> sent)
    {
        var notifier = new Mock<IRuleNotifier>();
        notifier.Setup(n => n.Notify(It.IsAny<RuleNotification>(), It.IsAny<CancellationToken>()))
            .Callback<RuleNotification, CancellationToken>((notification, _) => sent.Add(notification))
            .ReturnsAsync(GenericResult.Success());
        return notifier;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ANotifyStepIsClaimedBeforeItSendsAndSendsOnce()
    {
        var sent = new List<RuleNotification>();
        var graph = Steps(SourceFlow("flow"), Notify("tell", "ops", "AfterLoad", "done", ordinal: 1));
        var context = Context(graph, new PagedSourceGateway(PagedSourceGateway.Rows(2), []), Notifier(sent).Object);

        var built = GraphFlowBuilder.Build(graph, context);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        sent.ShouldHaveSingleItem();
        ((InMemoryStepClaims)context.StepClaims!).Made.ShouldHaveSingleItem().Component.ShouldBe("tell");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ATestRunBuildsTheStepAsOneThatDoesNothingAndClaimsNothing()
    {
        var sent = new List<RuleNotification>();
        var graph = Steps(SourceFlow("flow"), Notify("tell", "ops", "AfterLoad", "done", ordinal: 1));
        var context = Context(graph, new PagedSourceGateway(PagedSourceGateway.Rows(2), []), Notifier(sent).Object, new GraphTestRun(5, true));

        var built = GraphFlowBuilder.Build(graph, context);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        await built.Value!.Run(StepContext(new FlowVariables(), new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        sent.ShouldBeEmpty();
        ((InMemoryStepClaims)context.StepClaims!).Made.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStepWithNoOutsideEffectIsNotClaimed()
    {
        var graph = Steps(SetVariable("set", "Total", "Int32", "1"));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "Total", DataType = "Int32", ValueText = "0" });
        var context = ContextWith(graph, new Dictionary<string, object?>());

        var built = GraphFlowBuilder.Build(graph, context);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var variables = new FlowVariables();
        variables.Declare("Total", 0, "Int32");
        await built.Value!.Run(StepContext(variables, new FlowRunTally(new FlowExecutor())), TestContext.Current.CancellationToken);

        ((InMemoryStepClaims)context.StepClaims!).Made.ShouldBeEmpty();
    }
}
