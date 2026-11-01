using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the Notify step (FDW-1615): it sends through its rule when the run reaches it, with the run's counts in the message; one
/// set to fire on failure does nothing when reached and sends when a later step fails; and what cannot work is refused before a run.
/// </summary>
public sealed class NotifyStepTests
{
    private readonly FlowExecutor _executor = new();

    private static (Mock<IRuleNotifier> Notifier, List<RuleNotification> Sent) Notifier()
    {
        var sent = new List<RuleNotification>();
        var notifier = new Mock<IRuleNotifier>();
        notifier.Setup(n => n.Notify(It.IsAny<RuleNotification>(), It.IsAny<CancellationToken>()))
            .Callback<RuleNotification, CancellationToken>((notification, _) => sent.Add(notification))
            .ReturnsAsync(GenericResult.Success());
        return (notifier, sent);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANotifyStepSendsItsMessageWithTheRunsCountsWhenTheRunReachesIt()
    {
        var (notifier, sent) = Notifier();
        var events = new List<string>();
        var gateway = new PagedSourceGateway(PagedSourceGateway.Rows(5), events);
        var graph = Steps(SourceFlow("flow"), Notify("tell", "ops", "AfterLoad", "{{PipelineName}} read {{RowsExtracted}} rows, outcome {{Outcome}}", ordinal: 1));

        var built = GraphFlowBuilder.Build(graph, Context(graph, gateway, notifier.Object));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        var tally = new FlowRunTally(_executor);
        var ran = await built.Value!.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        var notification = sent.ShouldHaveSingleItem();
        notification.RuleName.ShouldBe("ops");
        notification.Message.ShouldBe("Sample read 5 rows, outcome Succeeded");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANotifyStepSetToFireOnFailureDoesNothingWhenReachedAndSendsWhenALaterStepFails()
    {
        var (notifier, sent) = Notifier();
        var run = new FlowNotifyRun(Guid.NewGuid(), "Sample", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var onFailure = new FlowNotify("alarm", notifier.Object, "ops", RunPoints.ByName("OnFailure"), "{{PipelineName}} {{Outcome}}", run, () => 0);
        var pipeline = new FlowPipeline("Sample", [onFailure, new FailingStep()]);
        var tally = new FlowRunTally(_executor);

        var ran = await pipeline.Run(StepContext(new FlowVariables(), tally), TestContext.Current.CancellationToken);

        ran.IsFailure.ShouldBeTrue();
        sent.ShouldHaveSingleItem().Message.ShouldBe("Sample Failed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANotifyStepSetToFireOnFailureSendsNothingWhenTheRunSucceeds()
    {
        var (notifier, sent) = Notifier();
        var run = new FlowNotifyRun(Guid.NewGuid(), "Sample", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var onFailure = new FlowNotify("alarm", notifier.Object, "ops", RunPoints.ByName("OnFailure"), "failed", run, () => 0);
        var pipeline = new FlowPipeline("Sample", [onFailure, new PassingStep()]);

        var ran = await pipeline.Run(StepContext(new FlowVariables(), new FlowRunTally(_executor)), TestContext.Current.CancellationToken);

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
        sent.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANotifyStepIsRefusedWhenTheHostHasNoNotificationService()
    {
        var graph = Steps(SourceFlow("flow"), Notify("tell", "ops", "AfterLoad", "done", ordinal: 1));

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("notification service");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANotifyStepThatFiresOnFailureInsideALoopIsAProblemAndAnUnknownVariableIsAProblem()
    {
        var graph = Steps(
            ForLoop("again", "i", 1, 3),
            Notify("alarm", "ops", "OnFailure", "failed", ordinal: 0, parent: "again"),
            Notify("tell", "ops", "AfterLoad", "{{Nonsense}}", ordinal: 1));
        graph.Variables.Add(new PipelineVariableConfiguration { Name = "i", DataType = "Int32", ValueText = "0" });

        var problems = GraphComponentValidator.Problems(graph, [], NullLogger.Instance);

        problems.ShouldContain(problem => problem.Reason.Contains("alarm") && problem.Reason.Contains("top of the pipeline"));
        problems.ShouldContain(problem => problem.Reason.Contains("Nonsense"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ANotifyStepWithAnUnknownRunPointIsAProblem()
    {
        var graph = Steps(SourceFlow("flow"), Notify("tell", "ops", "Sometime", "done", ordinal: 1));

        var problems = GraphComponentValidator.Problems(graph, [], NullLogger.Instance);

        problems.ShouldContain(problem => problem.Reason.Contains("Sometime"));
    }

    private sealed class FailingStep : IFlowStep
    {
        public string Name => "fail";

        public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult>(GenericResult.Failure(new Fdw.Messages.GenericMessage("the step failed")));
    }

    private sealed class PassingStep : IFlowStep
    {
        public string Name => "pass";

        public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
