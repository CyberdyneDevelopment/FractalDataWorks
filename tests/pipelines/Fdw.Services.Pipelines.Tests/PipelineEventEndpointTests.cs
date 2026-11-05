using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// The door a host that serves no hub uses to hand a pipeline event to the host that does (FDW-3773):
/// each kind reaches the host's own broadcaster with the arguments the in-process call would have had,
/// and a request that names no kind or lacks the kind's body is refused with nothing broadcast.
/// </summary>
[Trait("Category", "Api")]
public sealed class PipelineEventEndpointTests
{
    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Org = new("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid TaskId = new("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid OtherTaskId = new("cccccccc-0000-0000-0000-000000000002");

    private sealed class Door(IPipelineStatusBroadcaster broadcaster)
        : PipelineEventEndpointBase(broadcaster, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static async Task<int> Post(Mock<IPipelineStatusBroadcaster> broadcaster, PipelineEventRequest request)
    {
        var endpoint = Factory.Create<Door>(broadcaster.Object);
        await endpoint.HandleAsync(request, CancellationToken.None);
        return endpoint.HttpContext.Response.StatusCode;
    }

    private static Mock<IPipelineStatusBroadcaster> Broadcaster()
        => new(MockBehavior.Strict);

    [Fact]
    [Trait("Priority", "P1")]
    public async Task StatusChangedReachesTheBroadcasterWithTheOrgAndTheUpdate()
    {
        var broadcaster = Broadcaster();
        broadcaster.Setup(b => b.BroadcastStatusChange("Nightly", Execution, "Running", "started", Org)).Returns(Task.CompletedTask);

        var status = await Post(broadcaster, new PipelineEventRequest
        {
            Kind = "StatusChanged",
            OrgId = Org,
            Status = new PipelineStatusUpdate { PipelineName = "Nightly", ExecutionId = Execution, Status = "Running", Message = "started" },
        });

        status.ShouldBe(200);
        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AnAbsentOrgIsPassedOnAbsentNotInvented()
    {
        var broadcaster = Broadcaster();
        broadcaster.Setup(b => b.BroadcastStatusChange("Nightly", Execution, "Running", null, null)).Returns(Task.CompletedTask);

        var status = await Post(broadcaster, new PipelineEventRequest
        {
            Kind = "StatusChanged",
            Status = new PipelineStatusUpdate { PipelineName = "Nightly", ExecutionId = Execution, Status = "Running" },
        });

        status.ShouldBe(200);
        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ProgressReachesTheBroadcasterWithItsCounters()
    {
        var broadcaster = Broadcaster();
        broadcaster.Setup(b => b.BroadcastProgress("Nightly", Execution, 10, 9, 8, 1, 80, Org)).Returns(Task.CompletedTask);

        var status = await Post(broadcaster, new PipelineEventRequest
        {
            Kind = "Progress",
            OrgId = Org,
            Progress = new PipelineProgressUpdate
            {
                PipelineName = "Nightly", ExecutionId = Execution, RecordsExtracted = 10, RecordsTransformed = 9,
                RecordsLoaded = 8, RecordsFailed = 1, ProgressPercentage = 80,
            },
        });

        status.ShouldBe(200);
        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task CompletedReachesTheBroadcasterWithTheCompletionAndTheOrg()
    {
        var broadcaster = Broadcaster();
        var completion = new PipelineExecutionComplete { PipelineName = "Nightly", ExecutionId = Execution, Success = true, Status = "Completed" };
        broadcaster.Setup(b => b.BroadcastCompletion(completion, Org)).Returns(Task.CompletedTask);

        var status = await Post(broadcaster, new PipelineEventRequest { Kind = "Completed", OrgId = Org, Completion = completion });

        status.ShouldBe(200);
        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task TaskStatusReachesTheBroadcasterWithItsCounters()
    {
        var broadcaster = Broadcaster();
        broadcaster.Setup(b => b.BroadcastTaskStatus(Execution, TaskId, "Running", 5, 4, 1, 2, true)).Returns(Task.CompletedTask);

        var status = await Post(broadcaster, new PipelineEventRequest
        {
            Kind = "TaskStatus",
            Task = new PipelineTaskStatusUpdate
            {
                ExecutionId = Execution, TaskId = TaskId, Status = "Running", RecordsIn = 5, RecordsOut = 4,
                RecordsDiscarded = 1, RecordsHeld = 2, SampleBufferAtCapacity = true,
            },
        });

        status.ShouldBe(200);
        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task EdgeFlowReachesTheBroadcasterWithBothEnds()
    {
        var broadcaster = Broadcaster();
        broadcaster.Setup(b => b.BroadcastEdgeFlow(Execution, TaskId, OtherTaskId, 42)).Returns(Task.CompletedTask);

        var status = await Post(broadcaster, new PipelineEventRequest
        {
            Kind = "EdgeFlow",
            EdgeFlow = new PipelineEdgeFlowUpdate { ExecutionId = Execution, SourceTaskId = TaskId, TargetTaskId = OtherTaskId, RecordsFlowed = 42 },
        });

        status.ShouldBe(200);
        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ComponentsChangedReachesTheBroadcasterWithTheListAndTheOrg()
    {
        var broadcaster = Broadcaster();
        var update = new PipelineComponentsUpdate { PipelineName = "Nightly", ExecutionId = Execution, Components = [new() { Name = "src", Rows = 3, RowsIn = 0 }] };
        broadcaster.Setup(b => b.BroadcastComponents(update, Org)).Returns(Task.CompletedTask);

        var status = await Post(broadcaster, new PipelineEventRequest { Kind = "ComponentsChanged", OrgId = Org, Components = update });

        status.ShouldBe(200);
        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task PausedAndResumedReachTheBroadcasterWithTheExecution()
    {
        var broadcaster = Broadcaster();
        broadcaster.Setup(b => b.BroadcastExecutionPaused(Execution)).Returns(Task.CompletedTask);
        broadcaster.Setup(b => b.BroadcastExecutionResumed(Execution)).Returns(Task.CompletedTask);

        (await Post(broadcaster, new PipelineEventRequest { Kind = "Paused", ExecutionId = Execution })).ShouldBe(200);
        (await Post(broadcaster, new PipelineEventRequest { Kind = "Resumed", ExecutionId = Execution })).ShouldBe(200);

        broadcaster.VerifyAll();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AKindThatIsNotDeclaredIsRefusedAndNothingIsBroadcast()
    {
        var broadcaster = Broadcaster();

        var status = await Post(broadcaster, new PipelineEventRequest { Kind = "Exploded", ExecutionId = Execution });

        status.ShouldBe(400);
        broadcaster.VerifyNoOtherCalls();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("StatusChanged")]
    [InlineData("Progress")]
    [InlineData("Completed")]
    [InlineData("TaskStatus")]
    [InlineData("EdgeFlow")]
    [InlineData("ComponentsChanged")]
    [InlineData("Paused")]
    [InlineData("Resumed")]
    public async Task AKindWithoutItsBodyIsRefusedAndNothingIsBroadcast(string kind)
    {
        var broadcaster = Broadcaster();

        var status = await Post(broadcaster, new PipelineEventRequest { Kind = kind });

        status.ShouldBe(400);
        broadcaster.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void TheCollectionDeclaresExactlyTheEightKindsTheBroadcasterCanSend()
    {
        PipelineEventKinds.All().Select(k => k.Name).OrderBy(n => n, StringComparer.Ordinal).ShouldBe(
            ["Completed", "ComponentsChanged", "EdgeFlow", "Paused", "Progress", "Resumed", "StatusChanged", "TaskStatus"]);
    }
}
