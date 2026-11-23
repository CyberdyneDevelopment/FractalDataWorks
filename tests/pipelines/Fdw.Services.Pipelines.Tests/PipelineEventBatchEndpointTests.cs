using System;
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
/// The door a host that serves no hub uses to hand the events of one tick to the host that does (FDW-5249): each event reaches the host's own
/// broadcaster in order, and a batch with an event it cannot relay is refused at that event, naming its index.
/// </summary>
[Trait("Category", "Api")]
public sealed class PipelineEventBatchEndpointTests
{
    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Org = new("bbbbbbbb-0000-0000-0000-000000000001");

    private sealed class Door(IPipelineStatusBroadcaster broadcaster)
        : PipelineEventBatchEndpointBase(broadcaster, NullLogger.Instance)
    {
        protected override void ConfigureEndpoint() => AllowAnonymous();
    }

    private static async Task<(int Status, PipelineEventBatchResponse? Response)> Post(Mock<IPipelineStatusBroadcaster> broadcaster, params PipelineEventRequest[] events)
    {
        var endpoint = Factory.Create<Door>(broadcaster.Object);
        await endpoint.HandleAsync(new PipelineEventBatchRequest { Events = [.. events] }, CancellationToken.None);
        return (endpoint.HttpContext.Response.StatusCode, endpoint.Response);
    }

    private static PipelineEventRequest Status(string status)
        => new() { Kind = "StatusChanged", OrgId = Org, Status = new PipelineStatusUpdate { PipelineName = "Nightly", ExecutionId = Execution, Status = status } };

    [Fact]
    [Trait("Priority", "P0")]
    public async Task EveryEventReachesTheBroadcasterInOrderAndTheAnswerCountsThem()
    {
        var order = new System.Collections.Generic.List<string>();
        var broadcaster = new Mock<IPipelineStatusBroadcaster>(MockBehavior.Strict);
        broadcaster.Setup(b => b.BroadcastStatusChange("Nightly", Execution, It.IsAny<string>(), null, Org))
            .Callback<string, Guid, string, string?, Guid?>((_, _, s, _, _) => order.Add(s)).Returns(Task.CompletedTask);
        broadcaster.Setup(b => b.BroadcastExecutionPaused(Execution)).Callback(() => order.Add("Paused")).Returns(Task.CompletedTask);

        var (status, response) = await Post(
            broadcaster, Status("Running"), new PipelineEventRequest { Kind = "Paused", ExecutionId = Execution }, Status("Completed"));

        status.ShouldBe(200);
        response!.Relayed.ShouldBe(3);
        order.ShouldBe(["Running", "Paused", "Completed"]);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task AnEmptyBatchIsRelayedAsNoneAndBroadcastsNothing()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>(MockBehavior.Strict);

        var (status, response) = await Post(broadcaster);

        status.ShouldBe(200);
        response!.Relayed.ShouldBe(0);
        broadcaster.VerifyNoOtherCalls();
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AnUnknownKindStopsTheBatchThereAndTheEventsBeforeItWereBroadcast()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>(MockBehavior.Strict);
        broadcaster.Setup(b => b.BroadcastStatusChange("Nightly", Execution, "Running", null, Org)).Returns(Task.CompletedTask);

        var (status, _) = await Post(
            broadcaster, Status("Running"), new PipelineEventRequest { Kind = "Exploded", ExecutionId = Execution }, Status("Completed"));

        status.ShouldBe(400);
        broadcaster.Verify(b => b.BroadcastStatusChange(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Guid?>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AKindWithoutItsBodyStopsTheBatchThere()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>(MockBehavior.Strict);

        var (status, _) = await Post(broadcaster, new PipelineEventRequest { Kind = "StatusChanged" }, Status("Completed"));

        status.ShouldBe(400);
        broadcaster.VerifyNoOtherCalls();
    }
}
