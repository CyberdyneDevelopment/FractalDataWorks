using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>A run's record counters advanced.</summary>
[TypeOption(typeof(PipelineEventKinds), "Progress")]
public sealed class ProgressPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ProgressPipelineEventKind"/> class.</summary>
    public ProgressPipelineEventKind() : base("Progress", "A run's record counters advanced.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.Progress is { } update)
        {
            await broadcaster.BroadcastProgress(update.PipelineName, update.ExecutionId, update.RecordsExtracted, update.RecordsTransformed, update.RecordsLoaded, update.RecordsFailed, update.ProgressPercentage, request.OrgId).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
