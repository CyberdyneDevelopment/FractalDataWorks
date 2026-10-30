using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for what a pipeline run claims from the scheduler (FDW-3085, FDW-3117): a test run writes no table but still carries its pipeline's
/// cap, a pipeline that cannot be read as a graph runs alone with no cap, a graph that does not state its parallel runs fails the run, and
/// runs of one pipeline are held to the limit the pipeline states.
/// </summary>
public sealed class PipelineRunClaimsTests
{
    private static readonly PipelineRunLimitValues Limits = new(8, 8);
    private static readonly LoadTargetResolver NoTargets = new(null, null);

    private static PipelineExecutionRequest Request(Guid pipelineId, int? testRows = null) => new()
    {
        ExecutionId = Guid.NewGuid(),
        PipelineId = pipelineId,
        PipelineName = "Monthly",
        TriggerSource = "Test",
        RunRoles = ["DataWriter"],
        TestRowsPerSource = testRows
    };

    private static GraphPipelineConfiguration Graph(int maxParallelRuns, string overflowBehavior) => new()
    {
        Name = "Monthly",
        MaxParallelRuns = maxParallelRuns,
        OverflowBehavior = overflowBehavior
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATestRunWritesNoTableAndCarriesItsPipelinesCap()
    {
        var pipelineId = Guid.NewGuid();

        var claim = await PipelineRunClaims.For(
            Request(pipelineId, testRows: 10), Graph(3, "Refuse"), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken);

        claim.IsSuccess.ShouldBeTrue(claim.CurrentMessage);
        claim.Value!.Alone.ShouldBeFalse();
        claim.Value.WriteKeys.ShouldBeEmpty();
        claim.Value.Cap.ShouldBe(new PipelineRunCap($"pipeline/{pipelineId}", 3, true));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatCannotBeReadAsAGraphRunsAloneWithNoCap()
    {
        var claim = await PipelineRunClaims.For(
            Request(Guid.NewGuid()), Mock.Of<IPipelineImplementationConfiguration>(), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken);

        claim.IsSuccess.ShouldBeTrue(claim.CurrentMessage);
        claim.Value!.Alone.ShouldBeTrue();
        claim.Value.Cap.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatQueuesCarriesACapThatDoesNotRefuse()
    {
        var pipelineId = Guid.NewGuid();

        var claim = await PipelineRunClaims.For(
            Request(pipelineId, testRows: 1), Graph(1, "Queue"), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken);

        claim.Value!.Cap.ShouldBe(new PipelineRunCap($"pipeline/{pipelineId}", 1, false));
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData(0, "Queue", "maxParallelRuns")]
    [InlineData(-2, "Refuse", "maxParallelRuns")]
    [InlineData(1, "", "overflowBehavior")]
    [InlineData(1, "Delay", "overflowBehavior")]
    public async Task AGraphThatDoesNotStateItsParallelRunsFailsTheRunNamingTheValue(int maxParallelRuns, string overflowBehavior, string value)
    {
        var claim = await PipelineRunClaims.For(
            Request(Guid.NewGuid(), testRows: 1), Graph(maxParallelRuns, overflowBehavior), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken);

        claim.IsFailure.ShouldBeTrue();
        var message = claim.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain(value);
        message.ShouldContain("Monthly");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TwoRunsOfAPipelineLimitedToOneTakeTurnsAndARunOfAnotherDoesNot()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var monthly = Guid.NewGuid();
        var first = await scheduler.Enter(
            (await PipelineRunClaims.For(Request(monthly, 1), Graph(1, "Queue"), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!,
            TestContext.Current.CancellationToken);

        var again = scheduler.Enter(
            (await PipelineRunClaims.For(Request(monthly, 1), Graph(1, "Queue"), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!,
            TestContext.Current.CancellationToken);
        var other = scheduler.Enter(
            (await PipelineRunClaims.For(Request(Guid.NewGuid(), 1), Graph(1, "Queue"), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!,
            TestContext.Current.CancellationToken);

        (await Task.WhenAny(other, Task.Delay(150, TestContext.Current.CancellationToken))).ShouldBe(other);
        (await Task.WhenAny(again, Task.Delay(150, TestContext.Current.CancellationToken))).ShouldNotBe(again);
        scheduler.Release(first);
        (await Task.WhenAny(again, Task.Delay(150, TestContext.Current.CancellationToken))).ShouldBe(again);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineLimitedToTwoRunsTwoAtOnceAndRefusesTheThird()
    {
        var scheduler = new PipelineRunScheduler(TimeProvider.System);
        var monthly = Guid.NewGuid();
        var claim = (await PipelineRunClaims.For(Request(monthly, 1), Graph(2, "Refuse"), NoTargets, Limits, NullLogger.Instance, TestContext.Current.CancellationToken)).Value!;
        await scheduler.Enter(claim, TestContext.Current.CancellationToken);
        await scheduler.Enter(claim, TestContext.Current.CancellationToken);

        var third = scheduler.Queue(claim);

        third.IsRefused.ShouldBeTrue();
        third.RefusedLimit.ShouldBe(2);
        scheduler.Running.ShouldBe(2);
    }
}
