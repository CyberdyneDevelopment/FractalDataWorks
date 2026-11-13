using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Data.DataSets.Tests.Statuses;

/// <summary>
/// FDW-4488: a data set's status is the highest-ranked option of DataSetStatuses whose rule applies to its writers' latest runs. The rules read only the input.
/// </summary>
public sealed class DataSetStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Window = TimeSpan.FromHours(24);

    private static DataSetWriterRun Writer(string name, IExecutionStateType? state = null, TimeSpan? completedAgo = null)
    {
        if (state is null)
            return new DataSetWriterRun(name, true, null);

        var run = new Mock<IExecutionItem>();
        run.SetupGet(r => r.State).Returns(state);
        run.SetupGet(r => r.CompletedAt).Returns(completedAgo is { } ago ? Now - ago : null);
        return new DataSetWriterRun(name, true, run.Object);
    }

    private static string StatusOf(params DataSetWriterRun[] writers)
        => DataSetStatusResolver.Resolve("Orders", new DataSetStatusInput(writers, Window, Now), NullLogger.Instance).Value!.Name;

    [Fact]
    public void ADataSetNoPipelineWritesIsUnwritten() => StatusOf().ShouldBe("Unwritten");

    [Fact]
    public void AFailedLatestRunMakesTheDataSetFailedEvenWhenAnotherWriterIsRunning()
        => StatusOf(Writer("A", ExecutionStateTypes.Failed, TimeSpan.FromHours(1)), Writer("B", ExecutionStateTypes.Running)).ShouldBe("Failed");

    [Theory]
    [InlineData("Running")]
    [InlineData("Retrying")]
    [InlineData("Paused")]
    public void AnExecutingLatestRunMakesTheDataSetRunning(string state)
        => StatusOf(Writer("A", ExecutionStateTypes.ByName(state))).ShouldBe("Running");

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("TimedOut")]
    [InlineData("Interrupted")]
    public void AStoppedLatestRunMakesTheDataSetWarning(string state)
        => StatusOf(Writer("A", ExecutionStateTypes.ByName(state), TimeSpan.FromHours(1))).ShouldBe("Warning");

    [Fact]
    public void ACompletedRunInsideTheWindowMakesTheDataSetCurrent()
        => StatusOf(Writer("A", ExecutionStateTypes.Completed, TimeSpan.FromHours(23))).ShouldBe("Current");

    [Fact]
    public void ACompletedRunOlderThanTheWindowLeavesTheDataSetIdle()
        => StatusOf(Writer("A", ExecutionStateTypes.Completed, TimeSpan.FromHours(25))).ShouldBe("Idle");

    [Fact]
    public void WritersThatNeverRanOrOnlyWaitLeaveTheDataSetIdle()
        => StatusOf(Writer("A"), Writer("B", ExecutionStateTypes.Scheduled)).ShouldBe("Idle");

    [Fact]
    public void OneRecentCompletedRunIsEnoughToBeCurrentWhateverTheOtherWritersDid()
        => StatusOf(Writer("A", ExecutionStateTypes.Completed, TimeSpan.FromHours(2)), Writer("B"), Writer("C", ExecutionStateTypes.Completed, TimeSpan.FromDays(30))).ShouldBe("Current");

    [Fact]
    public void TheRanksOrderTheStatusesAndLeaveGapsForADevelopersOption()
    {
        var ranked = DataSetStatuses.All().OrderByDescending(status => status.Rank).Select(status => status.Name).ToList();

        ranked.ShouldBe(["NotApplicable", "Unwritten", "Failed", "Running", "Warning", "Current", "Idle"]);
        DataSetStatuses.All().Select(status => status.Rank).Distinct().Count().ShouldBe(DataSetStatuses.All().Count);
    }

    [Fact]
    public void TheStatusAKindStatesIsNeverPickedFromWritersSoNoExistingDataSetsStatusChanges()
    {
        DataSetStatuses.NotApplicable.IsStatedByKind.ShouldBeTrue();
        DataSetStatuses.NotApplicable.Applies(new DataSetStatusInput([], Window, Now)).ShouldBeFalse();
        DataSetStatuses.NotApplicable.Applies(new DataSetStatusInput([Writer("A", ExecutionStateTypes.Completed, TimeSpan.FromHours(1))], Window, Now)).ShouldBeFalse();
        DataSetStatuses.All().Where(status => status.IsStatedByKind).Select(status => status.Name).ShouldBe(["NotApplicable"]);
        StatusOf().ShouldBe("Unwritten");
    }

    [Fact]
    public void TheNotFoundSentinelNeverApplies()
        => DataSetStatuses.NotFound.Applies(new DataSetStatusInput([], Window, Now)).ShouldBeFalse();
}
