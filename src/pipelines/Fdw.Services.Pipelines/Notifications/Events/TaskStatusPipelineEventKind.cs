using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>A component of the run changed status.</summary>
[TypeOption(typeof(PipelineEventKinds), "TaskStatus")]
public sealed class TaskStatusPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TaskStatusPipelineEventKind"/> class.</summary>
    public TaskStatusPipelineEventKind() : base("TaskStatus", "A component of the run changed status.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.Task is { } update)
        {
            await broadcaster.BroadcastTaskStatus(update.ExecutionId, update.TaskId, update.Status, update.RecordsIn, update.RecordsOut, update.RecordsDiscarded, update.RecordsHeld, update.SampleBufferAtCapacity).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
