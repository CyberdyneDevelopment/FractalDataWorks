using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Hubs;
using Fdw.ServiceTypes;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Covers <see cref="PipelineStatusBroadcaster"/> branches not already exercised by
/// <see cref="PipelineStatusBroadcasterOrgFirehoseTests"/>: the coalescing rate the pipeline hub
/// declares (FDW-3956), the Hz-based coalescing window for per-task/per-edge broadcasts (first-call-always-sent,
/// rapid-repeat-is-coalesced, terminal-status-always-bypasses-coalescing), and the firehose targeting
/// for the remaining broadcast verbs (<c>BroadcastProgress</c>/<c>BroadcastExecutionPaused</c>/
/// <c>BroadcastExecutionResumed</c>).
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class PipelineStatusBroadcasterTests
{
    private static (PipelineStatusBroadcaster Broadcaster, List<string> Groups, Mock<IPipelineStatusHubClient> Client)
        CreateBroadcaster()
    {
        var targeted = new List<string>();
        var client = new Mock<IPipelineStatusHubClient>();
        var clients = new Mock<IHubClients<IPipelineStatusHubClient>>();
        clients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string g) => { targeted.Add(g); return client.Object; });
        var hubContext = new Mock<IHubContext<PipelineStatusHub, IPipelineStatusHubClient>>();
        hubContext.SetupGet(h => h.Clients).Returns(clients.Object);
        var broadcaster = new PipelineStatusBroadcaster(
            hubContext.Object,
            NullLogger<PipelineStatusBroadcaster>.Instance);
        return (broadcaster, targeted, client);
    }

    private static int GetBroadcastHz(PipelineStatusBroadcaster broadcaster) =>
        (int)typeof(PipelineStatusBroadcaster)
            .GetField("_broadcastHz", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(broadcaster)!;

    // ------------------------------------------------------------------
    // Coalescing rate: the pipeline hub's declared value (FDW-3956)
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void TheCoalescingRateIsThePipelineHubsDeclaredValue()
    {
        var (broadcaster, _, _) = CreateBroadcaster();

        ServiceTypeDefaults.TryResolve(new PipelineHubOption(), PipelineHubOption.BroadcastHz, out var declared).ShouldBeTrue();
        GetBroadcastHz(broadcaster).ShouldBe(declared);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ThePipelineHubDeclaresFiveBroadcastsASecond()
        => new PipelineHubOption().DeclaredDefaults[PipelineHubOption.BroadcastHz].ShouldBe(5);

    // ------------------------------------------------------------------
    // BroadcastProgress firehose targeting
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastProgressWithOrgTargetsOrgFirehose()
    {
        var (broadcaster, groups, client) = CreateBroadcaster();
        var orgId = Guid.NewGuid();
        var executionId = Guid.NewGuid();

        await broadcaster.BroadcastProgress("nfl", executionId, 10, 8, 6, 2, 50, orgId);

        groups.ShouldContain($"org:{orgId}:pipeline-updates");
        groups.ShouldContain("pipeline:nfl");
        groups.ShouldContain($"execution:{executionId}");
        client.Verify(c => c.OnProgressUpdated(It.Is<PipelineProgressUpdate>(u =>
            u.PipelineName == "nfl" &&
            u.ExecutionId == executionId &&
            u.RecordsExtracted == 10 &&
            u.RecordsTransformed == 8 &&
            u.RecordsLoaded == 6 &&
            u.RecordsFailed == 2 &&
            u.ProgressPercentage == 50)), Times.Exactly(3));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastProgressWithoutOrgTargetsNoFirehose()
    {
        var (broadcaster, groups, _) = CreateBroadcaster();
        var executionId = Guid.NewGuid();

        await broadcaster.BroadcastProgress("nfl", executionId, 1, 1, 1, 0, 100);

        groups.ShouldNotContain(g => g.StartsWith("org:", StringComparison.Ordinal));
        groups.ShouldContain("pipeline:nfl");
        groups.ShouldContain($"execution:{executionId}");
    }

    // ------------------------------------------------------------------
    // BroadcastCompletion
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastCompletionSendsThePayloadUnchangedToTheClient()
    {
        var (broadcaster, groups, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var completion = new PipelineExecutionComplete
        {
            PipelineName = "nfl",
            ExecutionId = executionId,
            Success = false,
            Status = "Failed",
            RecordsExtracted = 100,
            RecordsTransformed = 90,
            RecordsLoaded = 80,
            RecordsFailed = 10,
            DurationMs = 1234.5,
            ErrorMessage = "transform step threw",
        };

        await broadcaster.BroadcastCompletion(completion);

        groups.ShouldContain("pipeline:nfl");
        groups.ShouldContain($"execution:{executionId}");
        client.Verify(c => c.OnExecutionCompleted(It.Is<PipelineExecutionComplete>(u =>
            ReferenceEquals(u, completion) &&
            u.PipelineName == "nfl" &&
            u.ExecutionId == executionId &&
            u.Success == false &&
            u.Status == "Failed" &&
            u.RecordsExtracted == 100 &&
            u.RecordsTransformed == 90 &&
            u.RecordsLoaded == 80 &&
            u.RecordsFailed == 10 &&
            u.DurationMs == 1234.5 &&
            u.ErrorMessage == "transform step threw")), Times.Exactly(2));
    }

    // ------------------------------------------------------------------
    // BroadcastExecutionPaused / BroadcastExecutionResumed
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastExecutionPausedTargetsOnlyTheExecutionGroup()
    {
        var (broadcaster, groups, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();

        await broadcaster.BroadcastExecutionPaused(executionId);

        groups.ShouldBe([$"execution:{executionId}"]);
        client.Verify(c => c.OnExecutionPaused(executionId), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastExecutionResumedTargetsOnlyTheExecutionGroup()
    {
        var (broadcaster, groups, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();

        await broadcaster.BroadcastExecutionResumed(executionId);

        groups.ShouldBe([$"execution:{executionId}"]);
        client.Verify(c => c.OnExecutionResumed(executionId), Times.Once);
    }

    // ------------------------------------------------------------------
    // BroadcastTaskStatus coalescing
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastTaskStatusFirstCallIsAlwaysSent()
    {
        var (broadcaster, _, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        await broadcaster.BroadcastTaskStatus(executionId, taskId, "Running", 1, 1, 0, 0, false);

        client.Verify(c => c.OnTaskStatusChanged(It.IsAny<PipelineTaskStatusUpdate>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastTaskStatusRapidNonTerminalRepeatsAreCoalesced()
    {
        var (broadcaster, _, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        await broadcaster.BroadcastTaskStatus(executionId, taskId, "Running", 1, 1, 0, 0, false);
        await broadcaster.BroadcastTaskStatus(executionId, taskId, "Running", 2, 2, 0, 0, false);

        client.Verify(c => c.OnTaskStatusChanged(It.IsAny<PipelineTaskStatusUpdate>()), Times.Once);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Complete")]
    [InlineData("Failed")]
    [InlineData("Cancelled")]
    [InlineData("COMPLETE")]
    [InlineData("failed")]
    public async Task BroadcastTaskStatusTerminalStatusAlwaysBypassesCoalescing(string terminalStatus)
    {
        var (broadcaster, _, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        await broadcaster.BroadcastTaskStatus(executionId, taskId, terminalStatus, 1, 1, 0, 0, false);
        await broadcaster.BroadcastTaskStatus(executionId, taskId, terminalStatus, 2, 2, 0, 0, false);

        client.Verify(c => c.OnTaskStatusChanged(It.IsAny<PipelineTaskStatusUpdate>()), Times.Exactly(2));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastTaskStatusSendsToExecutionGroupOnlyWithExpectedPayload()
    {
        var (broadcaster, groups, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        await broadcaster.BroadcastTaskStatus(executionId, taskId, "Running", 10, 9, 1, 2, true);

        groups.ShouldBe([$"execution:{executionId}"]);
        client.Verify(c => c.OnTaskStatusChanged(It.Is<PipelineTaskStatusUpdate>(u =>
            u.ExecutionId == executionId &&
            u.TaskId == taskId &&
            u.Status == "Running" &&
            u.RecordsIn == 10 &&
            u.RecordsOut == 9 &&
            u.RecordsDiscarded == 1 &&
            u.RecordsHeld == 2 &&
            u.SampleBufferAtCapacity)), Times.Once);
    }

    // ------------------------------------------------------------------
    // BroadcastEdgeFlow coalescing
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastEdgeFlowFirstCallIsAlwaysSent()
    {
        var (broadcaster, groups, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var sourceTaskId = Guid.NewGuid();
        var targetTaskId = Guid.NewGuid();

        await broadcaster.BroadcastEdgeFlow(executionId, sourceTaskId, targetTaskId, 100);

        groups.ShouldBe([$"execution:{executionId}"]);
        client.Verify(c => c.OnEdgeFlow(It.Is<PipelineEdgeFlowUpdate>(u =>
            u.ExecutionId == executionId &&
            u.SourceTaskId == sourceTaskId &&
            u.TargetTaskId == targetTaskId &&
            u.RecordsFlowed == 100)), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastEdgeFlowRapidRepeatsAreCoalesced()
    {
        var (broadcaster, _, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var sourceTaskId = Guid.NewGuid();
        var targetTaskId = Guid.NewGuid();

        await broadcaster.BroadcastEdgeFlow(executionId, sourceTaskId, targetTaskId, 100);
        await broadcaster.BroadcastEdgeFlow(executionId, sourceTaskId, targetTaskId, 150);

        client.Verify(c => c.OnEdgeFlow(It.IsAny<PipelineEdgeFlowUpdate>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task BroadcastEdgeFlowDistinctEdgesCoalesceIndependently()
    {
        var (broadcaster, _, client) = CreateBroadcaster();
        var executionId = Guid.NewGuid();
        var taskA = Guid.NewGuid();
        var taskB = Guid.NewGuid();
        var taskC = Guid.NewGuid();

        await broadcaster.BroadcastEdgeFlow(executionId, taskA, taskB, 10);
        await broadcaster.BroadcastEdgeFlow(executionId, taskB, taskC, 20);

        client.Verify(c => c.OnEdgeFlow(It.IsAny<PipelineEdgeFlowUpdate>()), Times.Exactly(2));
    }
}
