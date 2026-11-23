using System;
using System.Threading.Tasks;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Pipelines.Tests;

/// <summary>
/// FDW-6140: a piece of a run's stream travels the relay like any other event: the host that runs the pipeline hands it to the host that serves the hub, which sends it to the group of the person it is addressed to.
/// The kind carries the piece and nothing else, and a request that names the kind without one sends nothing.
/// </summary>
public sealed class RunPieceEventKindTests
{
    private static readonly Guid Person = Guid.Parse("66666666-0000-0000-0000-000000000006");

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task APieceReachesTheBroadcasterOfTheHubHostUnchanged()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();
        var piece = new PipelineRunPieceUpdate { PersonId = Person, ClaimRootId = Guid.NewGuid(), Sequence = 42, Kind = "Text", Text = "Hello" };

        var relayed = await PipelineEventKinds.ByName("RunPiece").Relay(broadcaster.Object, new PipelineEventRequest { Kind = "RunPiece", RunPiece = piece });

        relayed.ShouldBeTrue();
        broadcaster.Verify(b => b.BroadcastRunPiece(piece), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ARequestThatNamesTheKindWithNoPieceSendsNothing()
    {
        var broadcaster = new Mock<IPipelineStatusBroadcaster>();

        var relayed = await PipelineEventKinds.ByName("RunPiece").Relay(broadcaster.Object, new PipelineEventRequest { Kind = "RunPiece" });

        relayed.ShouldBeFalse();
        broadcaster.Verify(b => b.BroadcastRunPiece(It.IsAny<PipelineRunPieceUpdate>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TheRelayBroadcasterHandsAPieceToTheHostThatServesTheHubAsAnEventOfThatKind()
    {
        PipelineEventRequest? sent = null;
        var client = new Mock<IPipelineEventClient>();
        client.Setup(c => c.Relay(It.IsAny<PipelineEventRequest>(), It.IsAny<System.Threading.CancellationToken>()))
            .Callback<PipelineEventRequest, System.Threading.CancellationToken>((request, _) => sent = request)
            .ReturnsAsync(Fdw.Results.GenericResult<PipelineEventResponse>.Success(new PipelineEventResponse()));
        var piece = new PipelineRunPieceUpdate { PersonId = Person, Sequence = 7, Kind = "StepStarted", StepName = "send" };

        await new RelayPipelineStatusBroadcaster(client.Object, NullLogger<RelayPipelineStatusBroadcaster>.Instance).BroadcastRunPiece(piece);

        var request = sent.ShouldNotBeNull();
        request.Kind.ShouldBe("RunPiece");
        request.RunPiece.ShouldBeSameAs(piece);
    }
}
