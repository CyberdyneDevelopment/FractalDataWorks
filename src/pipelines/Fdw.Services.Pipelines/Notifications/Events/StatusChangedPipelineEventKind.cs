using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>A run's status changed.</summary>
[TypeOption(typeof(PipelineEventKinds), "StatusChanged")]
public sealed class StatusChangedPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="StatusChangedPipelineEventKind"/> class.</summary>
    public StatusChangedPipelineEventKind() : base("StatusChanged", "A run's status changed.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.Status is { } update)
        {
            await broadcaster.BroadcastStatusChange(update.PipelineName, update.ExecutionId, update.Status, update.Message, request.OrgId).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
