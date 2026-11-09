using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Execution;
using Shouldly;
using Xunit;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// The counting of a pipeline's consecutive failures (FDW-3864): newest first, failures that count add one, runs that do not count are passed over, a success ends the count, and
/// runs before the pipeline was last enabled are not looked at.
/// </summary>
public sealed class PipelineFailureCounterTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private static PipelineRunOutcome Failed(int minutes, string category = "Validation", string code = "FDW-20000") =>
        new(Guid.NewGuid(), Start.AddMinutes(minutes), false, true, category, code, null);

    private static PipelineRunOutcome Transient(int minutes) => new(Guid.NewGuid(), Start.AddMinutes(minutes), false, false, "Dependency", "FDW-71000", null);

    private static PipelineRunOutcome Cancelled(int minutes) => new(Guid.NewGuid(), Start.AddMinutes(minutes), false, false, null, null, null);

    private static PipelineRunOutcome Succeeded(int minutes) => new(Guid.NewGuid(), Start.AddMinutes(minutes), true, false, null, null, null);

    private static IReadOnlyList<PipelineRunOutcome> NewestFirst(params PipelineRunOutcome[] runs) => [.. runs.OrderByDescending(run => run.At)];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ConsecutiveCountedFailuresAreCountedAndNamedNewestFirst()
    {
        var runs = NewestFirst(Failed(3), Failed(2), Failed(1));

        var found = PipelineFailureCounter.Count(runs, null);

        found.Count.ShouldBe(3);
        found.Failures.Select(failure => failure.At).ShouldBe([Start.AddMinutes(3), Start.AddMinutes(2), Start.AddMinutes(1)]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RunsThatAreAllFailuresLeaveTheCountOpenBecauseNothingEndedIt()
    {
        var found = PipelineFailureCounter.Count(NewestFirst(Failed(2), Failed(1)), null);

        found.Count.ShouldBe(2);
        found.Ended.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ASuccessEndsTheCountSoOlderFailuresAreNotCounted()
    {
        var runs = NewestFirst(Failed(5), Failed(4), Succeeded(3), Failed(2), Failed(1));

        var found = PipelineFailureCounter.Count(runs, null);

        found.Count.ShouldBe(2);
        found.Ended.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ATransientFailureOrACancelIsPassedOverWithoutAddingOrEndingTheCount()
    {
        var runs = NewestFirst(Failed(5), Transient(4), Failed(3), Cancelled(2), Failed(1));

        var found = PipelineFailureCounter.Count(runs, null);

        found.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void RunsBeforeThePipelinesLastChangeAreNotLookedAtAndEndTheCount()
    {
        var runs = NewestFirst(Failed(5), Failed(4), Failed(2), Failed(1));

        var found = PipelineFailureCounter.Count(runs, Start.AddMinutes(3));

        found.Count.ShouldBe(2);
        found.Ended.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NoRunsCountNothing()
    {
        var found = PipelineFailureCounter.Count([], null);

        found.Count.ShouldBe(0);
        found.Failures.ShouldBeEmpty();
    }

    private static PipelineRunOutcome TimedOut(int minutes) => new(Guid.NewGuid(), Start.AddMinutes(minutes), false, false, null, "TimedOut", null, true);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ARunThatTimedOutIsPassedOverUnlessTheThresholdCountsIt()
    {
        var runs = NewestFirst(TimedOut(3), Failed(2), Failed(1));

        PipelineFailureCounter.Count(runs, null, false).Count.ShouldBe(2);
        PipelineFailureCounter.Count(runs, null).Count.ShouldBe(2);
        PipelineFailureCounter.Count(runs, null, true).Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void ACountedTimeoutDoesNotEndTheCountAndASuccessStillDoes()
    {
        var runs = NewestFirst(TimedOut(3), Succeeded(2), Failed(1));

        var found = PipelineFailureCounter.Count(runs, null, true);

        found.Count.ShouldBe(1);
        found.Ended.ShouldBeTrue();
    }
}
