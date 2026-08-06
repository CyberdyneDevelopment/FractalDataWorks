using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// Covers <see cref="PipelineStatusBroadcaster"/> branches not already exercised by
/// <see cref="PipelineStatusBroadcasterOrgFirehoseTests"/>: the options-resolution fallback in the
/// constructor (zero/negative BroadcastHz and SampleBufferMaxBytes fall back to the documented
/// defaults), the Hz-based coalescing window for per-task/per-edge broadcasts (first-call-always-sent,
/// rapid-repeat-is-coalesced, terminal-status-always-bypasses-coalescing), and the firehose targeting
/// for the remaining broadcast verbs (<c>BroadcastProgress</c>/<c>BroadcastExecutionPaused</c>/
/// <c>BroadcastExecutionResumed</c>).
/// </summary>
[Trait("Category", "CoreFramework")]
public sealed class PipelineStatusBroadcasterTests
{
    private static (PipelineStatusBroadcaster Broadcaster, List<string> Groups, Mock<IPipelineStatusHubClient> Client)
        CreateBroadcaster(PipelineStatusBroadcasterOptions? options = null)
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
            NullLogger<PipelineStatusBroadcaster>.Instance,
            options is null ? null : Options.Create(options));
        return (broadcaster, targeted, client);
    }

    // Why: BroadcastHz/SampleBufferMaxBytes resolution has no public getter, so reflection is the
    // only way to assert the private-field outcome of the constructor's `> 0 ? value : default`
    // fallback branch without relying on real-time sleeps to prove the coalescing window width.
    private static int GetBroadcastHz(PipelineStatusBroadcaster broadcaster) =>
        (int)typeof(PipelineStatusBroadcaster)
            .GetField("_broadcastHz", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(broadcaster)!;

    private static long GetSampleBufferMaxBytes(PipelineStatusBroadcaster broadcaster) =>
        (long)typeof(PipelineStatusBroadcaster)
            .GetField("_sampleBufferMaxBytes", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(broadcaster)!;

    // ------------------------------------------------------------------
    // Constructor / options resolution
    // ------------------------------------------------------------------

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorWithNullOptionsResolvesDocumentedDefaults()
    {
        var (broadcaster, _, _) = CreateBroadcaster(options: null);

        GetBroadcastHz(broadcaster).ShouldBe(5);
        GetSampleBufferMaxBytes(broadcaster).ShouldBe(10_000_000L);
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConstructorWithNonPositiveBroadcastHzFallsBackToDefaultOfFive(int configuredHz)
    {
        var (broadcaster, _, _) = CreateBroadcaster(new PipelineStatusBroadcasterOptions { BroadcastHz = configuredHz });

        GetBroadcastHz(broadcaster).ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorWithPositiveBroadcastHzUsesConfiguredValue()
    {
        var (broadcaster, _, _) = CreateBroadcaster(new PipelineStatusBroadcasterOptions { BroadcastHz = 20 });

        GetBroadcastHz(broadcaster).ShouldBe(20);
    }

    [Theory]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void ConstructorWithNonPositiveSampleBufferFallsBackToDefaultOfTenMegabytes(long configuredBytes)
    {
        var (broadcaster, _, _) = CreateBroadcaster(new PipelineStatusBroadcasterOptions { SampleBufferMaxBytes = configuredBytes });

        GetSampleBufferMaxBytes(broadcaster).ShouldBe(10_000_000L);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorWithPositiveSampleBufferUsesConfiguredValue()
    {
        var (broadcaster, _, _) = CreateBroadcaster(new PipelineStatusBroadcasterOptions { SampleBufferMaxBytes = 42 });

        GetSampleBufferMaxBytes(broadcaster).ShouldBe(42L);
    }

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
        // Why: BroadcastCompletion targets two groups (pipeline:{name} + execution:{id}); the test
        // mock returns the same client for every group, so the unchanged payload arrives twice.
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

        // Why: a terminal status always sends regardless of the Hz cadence, so both calls above must
        // reach the client — proving IsTerminalStatus (case-insensitive) short-circuits ShouldCoalesce.
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

        // Why: the coalescing key includes source+target task IDs, so two distinct edges must not
        // suppress each other even though both broadcasts land in the same Hz window.
        client.Verify(c => c.OnEdgeFlow(It.IsAny<PipelineEdgeFlowUpdate>()), Times.Exactly(2));
    }
}
