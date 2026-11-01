using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Tests.Pipelines;
using static Fdw.Services.Etl.Tests.Flow.FlowTestKit;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// Tests for the run point of an Execute pipeline component (FDW-2043): after a stage or the load it starts the pipeline when the run reaches it, on
/// failure it does nothing when reached and starts the pipeline only when the run fails, and with a run point its parameter values may name the run's facts.
/// </summary>
public sealed class FlowExecutePipelineRunPointTests
{
    private static readonly FlowRunIdentity Identity = new(
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"), Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002"), ["etl-runner"]);

    private static FlowStepContext Context(FakeRunStarter starter) =>
        new(new FlowVariables(), new FlowRunTally(new FlowExecutor()), Options(), starter, Identity);

    private static FlowExecutePipeline Step(string runPoint) =>
        new(
            "tidy",
            "Cleanup",
            [new FlowStepParameter("Why", variables => VariableText.Render("{{Outcome}} in {{PipelineName}}", variables))],
            awaitCompletion: false,
            resultVariable: null,
            failOnChildFailure: false,
            logger: null,
            runPoint: RunPoints.ByName(runPoint),
            run: new FlowNotifyRun(Guid.NewGuid(), "Nightly", Guid.NewGuid(), DateTimeOffset.UtcNow),
            loaded: () => 0);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStepSetToOnFailureStartsNothingWhenReachedAndStartsThePipelineWhenTheRunFails()
    {
        var starter = new FakeRunStarter();
        var context = Context(starter);
        var step = Step("OnFailure");

        var reached = await step.Run(context, TestContext.Current.CancellationToken);
        reached.IsSuccess.ShouldBeTrue(reached.CurrentMessage);
        starter.Started.ShouldBeEmpty();

        var failed = await step.OnFailure(context, GenericResult.Failure(new Fdw.Messages.GenericMessage("the load failed")), TestContext.Current.CancellationToken);

        failed.IsSuccess.ShouldBeTrue(failed.CurrentMessage);
        var start = starter.Started.ShouldHaveSingleItem();
        start.PipelineName.ShouldBe("Cleanup");
        start.Parameters.ShouldBe([new KeyValuePair<string, string>("Why", "Failed in Nightly")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStepSetToAfterLoadStartsThePipelineWhenReachedAndNotAgainWhenTheRunFails()
    {
        var starter = new FakeRunStarter();
        var context = Context(starter);
        var step = Step("AfterLoad");

        var reached = await step.Run(context, TestContext.Current.CancellationToken);
        await step.OnFailure(context, GenericResult.Failure(new Fdw.Messages.GenericMessage("a later step failed")), TestContext.Current.CancellationToken);

        reached.IsSuccess.ShouldBeTrue(reached.CurrentMessage);
        var start = starter.Started.ShouldHaveSingleItem();
        start.Parameters.ShouldBe([new KeyValuePair<string, string>("Why", "Succeeded in Nightly")]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARunPointWithoutTheRunIsRefusedWhenTheStepIsBuilt()
    {
        Should.Throw<ArgumentException>(() => new FlowExecutePipeline(
            "tidy", "Cleanup", [], false, null, false, logger: null, runPoint: RunPoints.ByName("OnFailure"), run: null, loaded: null));
    }
}
