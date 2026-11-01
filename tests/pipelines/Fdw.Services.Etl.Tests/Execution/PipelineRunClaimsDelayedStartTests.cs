using System;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// Tests for the cap a run carries from a pipeline that holds a run past its limit (FDW-3129): the delay or time of day is read from the
/// pipeline, and one that cannot be read fails the run naming it, with nothing assumed.
/// </summary>
public sealed class PipelineRunClaimsDelayedStartTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 10, 0, 0, TimeSpan.Zero);

    private static Task<Fdw.Results.IGenericResult<PipelineRunClaim>> Claim(GraphPipelineConfiguration graph, Guid pipelineId) =>
        PipelineRunClaims.For(
            new PipelineExecutionRequest
            {
                ExecutionId = Guid.NewGuid(),
                PipelineId = pipelineId,
                PipelineName = "Monthly",
                TriggerSource = "Test",
                RunRoles = ["DataWriter"],
                TestRowsPerSource = 1
            },
            graph,
            new LoadTargetResolver(null, null),
            new PipelineRunLimitValues(8, 8),
            NullLogger.Instance,
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADelayedStartPipelineCarriesItsDelayInItsCap()
    {
        var graph = new GraphPipelineConfiguration { Name = "Monthly", MaxParallelRuns = 1, OverflowBehavior = "DelayedStart", DelayedStartSeconds = 30 };

        var claim = await Claim(graph, Guid.NewGuid());

        claim.IsSuccess.ShouldBeTrue(claim.CurrentMessage);
        var cap = claim.Value!.Cap.ShouldNotBeNull();
        cap.RefusesWhenFull.ShouldBeFalse();
        cap.Delay.ShouldNotBeNull().DueAt(Now).ShouldBe(Now.AddSeconds(30));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineThatQueuesCarriesNoDelay()
    {
        var graph = new GraphPipelineConfiguration { Name = "Monthly", MaxParallelRuns = 1, OverflowBehavior = "Queue" };

        var claim = await Claim(graph, Guid.NewGuid());

        claim.Value!.Cap.ShouldNotBeNull().Delay.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADelayedStartPipelineThatStatesNoDelayFailsTheRunNamingIt()
    {
        var graph = new GraphPipelineConfiguration { Name = "Monthly", MaxParallelRuns = 1, OverflowBehavior = "DelayedStart" };

        var claim = await Claim(graph, Guid.NewGuid());

        claim.IsFailure.ShouldBeTrue();
        var message = claim.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("delayedStart");
        message.ShouldContain("Monthly");
    }
}
