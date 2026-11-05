using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>Rows moved across a link between two components.</summary>
[TypeOption(typeof(PipelineEventKinds), "EdgeFlow")]
public sealed class EdgeFlowPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="EdgeFlowPipelineEventKind"/> class.</summary>
    public EdgeFlowPipelineEventKind() : base("EdgeFlow", "Rows moved across a link between two components.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.EdgeFlow is { } update)
        {
            await broadcaster.BroadcastEdgeFlow(update.ExecutionId, update.SourceTaskId, update.TargetTaskId, update.RecordsFlowed).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
