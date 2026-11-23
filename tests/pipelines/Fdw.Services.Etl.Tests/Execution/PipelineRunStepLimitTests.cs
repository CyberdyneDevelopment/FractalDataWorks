using System;
using System.Linq;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for <see cref="PipelineRunStepLimit"/> (FDW-6141, slice 2c): the step limit a flow takes its steps from. It answers success for each step the run's limits allow and,
/// for the first step they do not, the coded failure naming the pipeline, the step and the limit; an at-once child's steps count against its parent's.
/// </summary>
public sealed class PipelineRunStepLimitTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void EachStepTheLimitsAllowIsTakenAndTheFirstItDoesNotIsARefusalThatNamesThePipelineTheStepAndTheLimit()
    {
        var limits = PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 60, maxSteps: 2);
        var sut = new PipelineRunStepLimit(limits, "Chat", NullLogger.Instance);

        sut.TakeStep("lookup").IsSuccess.ShouldBeTrue();
        sut.TakeStep("answer").IsSuccess.ShouldBeTrue();
        var refused = sut.TakeStep("log");

        refused.IsFailure.ShouldBeTrue();
        refused.Messages.Select(message => message.Code).ShouldContain("PIPELINERUN-60845");
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("Chat");
        refused.CurrentMessage.ShouldNotBeNull().ShouldContain("log");
        limits.StepsLeft.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AChildsStepsAreTakenFromItsParentToo()
    {
        var parent = PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 60, maxSteps: 3);
        var child = new PipelineRunStepLimit(parent.ForChild(Start, maxRunSeconds: null, maxSteps: 10), "Child", NullLogger.Instance);

        child.TakeStep("one").IsSuccess.ShouldBeTrue();
        child.TakeStep("two").IsSuccess.ShouldBeTrue();
        child.TakeStep("three").IsSuccess.ShouldBeTrue();

        child.TakeStep("four").IsFailure.ShouldBeTrue();
        parent.StepsLeft.ShouldBe(0);
    }
}
