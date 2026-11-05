using System;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>A test execution was resumed.</summary>
[TypeOption(typeof(PipelineEventKinds), "Resumed")]
public sealed class ResumedPipelineEventKind : PipelineEventKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ResumedPipelineEventKind"/> class.</summary>
    public ResumedPipelineEventKind() : base("Resumed", "A test execution was resumed.")
    {
    }

    /// <inheritdoc />
    public override async Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request)
    {
        if (request.ExecutionId != Guid.Empty)
        {
            await broadcaster.BroadcastExecutionResumed(request.ExecutionId).ConfigureAwait(false);
            return true;
        }

        return false;
    }
}
