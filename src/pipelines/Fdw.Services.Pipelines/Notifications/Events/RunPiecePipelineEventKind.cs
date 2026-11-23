using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>A piece of a run's stream was written (FDW-6140): it reaches the group of the person the run is for.</summary>
[TypeOption(typeof(PipelineEventKinds), "RunPiece")]
public sealed class RunPiecePipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="RunPiecePipelineEventKind"/> class.</summary>
    public RunPiecePipelineEventKind() : base("RunPiece", "A piece of a run's stream was written.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.RunPiece is { } piece)
        {
            await broadcaster.BroadcastRunPiece(piece).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
