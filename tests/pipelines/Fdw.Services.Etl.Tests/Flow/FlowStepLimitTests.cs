using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Flow;

/// <summary>
/// The step limit of a run (FDW-6141, slice 2c): a step is taken from the run's limit before it runs, and a step the limit does not allow is not started: the run
/// ends with a coded failure and the steps before it have run.
/// </summary>
public sealed class FlowStepLimitTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

    private sealed class CountingStep(string name, List<string> ran) : IFlowStep
    {
        public string Name => name;

        public Task<IGenericResult> Run(FlowStepContext context, CancellationToken cancellationToken = default)
        {
            ran.Add(name);
            return Task.FromResult<IGenericResult>(GenericResult.Success());
        }
    }

    private static FlowStepContext Context(IFlowStepLimit? limit) =>
        new(new FlowVariables(), new FlowExecutor(), new FlowRunOptions { LinkCapacity = 2 }, null, null, null, null, null, limit);

    private static PipelineRunStepLimit Limit(int steps) =>
        new(PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 60, maxSteps: steps), "Chat", NullLogger.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunStopsAtTheFirstStepItsLimitDoesNotAllowWithACodedFailureAndTheStepsBeforeItHaveRun()
    {
        var ran = new List<string>();
        var steps = new FlowPipeline("run", [new CountingStep("a", ran), new CountingStep("b", ran), new CountingStep("c", ran), new CountingStep("d", ran)]);

        var result = await steps.Run(Context(Limit(2)), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-60845");
        ran.ShouldBe(["a", "b"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunWithinItsLimitRunsEveryStep()
    {
        var ran = new List<string>();
        var steps = new FlowPipeline("run", [new CountingStep("a", ran), new CountingStep("b", ran)]);

        var result = await steps.Run(Context(Limit(2)), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ran.ShouldBe(["a", "b"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARunThatStatesNoStepLimitIsNotLimited()
    {
        var ran = new List<string>();
        var steps = new FlowPipeline("run", [new CountingStep("a", ran), new CountingStep("b", ran), new CountingStep("c", ran)]);

        var result = await steps.Run(Context(null), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        ran.ShouldBe(["a", "b", "c"]);
    }
}
