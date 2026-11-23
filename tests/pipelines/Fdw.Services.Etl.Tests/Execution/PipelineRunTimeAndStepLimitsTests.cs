using System;
using System.Linq;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// FDW-6141 slice 2: one object carries a run's limits on time and steps (the brief's word, "limits"; "budget" stays free for tokens). A step is taken from it before it runs; a
/// child an at-once run starts holds the parent's object, so its steps count against the parent's and its deadline is the parent's unless its own pipeline states a shorter one:
/// a limit a child states can only shorten the turn's, never extend it. A stored value above the ceiling in force is refused with a code, never run with a lowered limit.
/// </summary>
[Trait("Category", "PipelineRunTimeAndStepLimits")]
public sealed class PipelineRunTimeAndStepLimitsTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Priority", "P0")]
    public void ARunTakesAsManyStepsAsItsLimitAllowsAndNoMore()
    {
        var limits = PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 60, maxSteps: 3);

        limits.TryTakeStep().ShouldBeTrue();
        limits.TryTakeStep().ShouldBeTrue();
        limits.TryTakeStep().ShouldBeTrue();
        limits.TryTakeStep().ShouldBeFalse();
        limits.StepsLeft.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void TheDeadlineIsTheStartPlusTheTimeLimit()
        => PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 90, maxSteps: 10).Deadline.ShouldBe(Start.AddSeconds(90));

    [Fact]
    [Trait("Priority", "P0")]
    public void AChildsStepsCountAgainstTheParentsAndItsOwnLimitCanOnlyShortenThem()
    {
        var parent = PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 60, maxSteps: 5);

        var child = parent.ForChild(Start.AddSeconds(1), maxRunSeconds: 600, maxSteps: 10);

        Enumerable.Range(0, 10).Count(_ => child.TryTakeStep()).ShouldBe(5);
        parent.StepsLeft.ShouldBe(0);
        parent.TryTakeStep().ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AChildThatStatesAShorterTimeAndFewerStepsKeepsTheShorterOnesAndTheParentKeepsTheRest()
    {
        var parent = PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 60, maxSteps: 5);

        var child = parent.ForChild(Start.AddSeconds(10), maxRunSeconds: 20, maxSteps: 2);

        child.Deadline.ShouldBe(Start.AddSeconds(30));
        child.TryTakeStep().ShouldBeTrue();
        child.TryTakeStep().ShouldBeTrue();
        child.TryTakeStep().ShouldBeFalse();
        parent.StepsLeft.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AChildWithNoLimitsOfItsOwnIsBoundedByTheParent()
    {
        var parent = PipelineRunTimeAndStepLimits.Start(Start, maxRunSeconds: 60, maxSteps: 2);

        var child = parent.ForChild(Start.AddSeconds(5), maxRunSeconds: null, maxSteps: null);

        child.Deadline.ShouldBe(Start.AddSeconds(60));
        child.TryTakeStep().ShouldBeTrue();
        child.TryTakeStep().ShouldBeTrue();
        child.TryTakeStep().ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AStoredValueAboveTheCeilingInForceIsRefusedNamingThePipelineTheValueAndTheCeiling()
    {
        var judged = PipelineRunLimitCeilings.Judge("Turn", maxRunSeconds: 900, maxSteps: 10, new PipelineRunLimitCeilings(MaxRunSeconds: 600, MaxSteps: 50), NullLogger.Instance);

        judged.IsFailure.ShouldBeTrue();
        var message = judged.Messages.ShouldHaveSingleItem();
        message.Code.ShouldBe("PIPELINERUN-20841");
        message.Message.ShouldContain("Turn");
        message.Message.ShouldContain("900");
        message.Message.ShouldContain("600");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void AValueAtOrBelowTheCeilingIsNotRefused()
        => PipelineRunLimitCeilings.Judge("Turn", maxRunSeconds: 600, maxSteps: 50, new PipelineRunLimitCeilings(MaxRunSeconds: 600, MaxSteps: 50), NullLogger.Instance).IsSuccess.ShouldBeTrue();
}
