using System;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>A test execution was paused.</summary>
[TypeOption(typeof(PipelineEventKinds), "Paused")]
public sealed class PausedPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="PausedPipelineEventKind"/> class.</summary>
    public PausedPipelineEventKind() : base("Paused", "A test execution was paused.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.ExecutionId != Guid.Empty)
        {
            await broadcaster.BroadcastExecutionPaused(request.ExecutionId).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
