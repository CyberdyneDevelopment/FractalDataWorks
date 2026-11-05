using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>A run finished.</summary>
[TypeOption(typeof(PipelineEventKinds), "Completed")]
public sealed class CompletedPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CompletedPipelineEventKind"/> class.</summary>
    public CompletedPipelineEventKind() : base("Completed", "A run finished.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.Completion is { } update)
        {
            await broadcaster.BroadcastCompletion(update, request.OrgId).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
