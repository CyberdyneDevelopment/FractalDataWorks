using System;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Hubs;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// The <see cref="IPipelineStatusBroadcaster"/> of a host that serves no hub: each broadcast becomes a
/// <see cref="PipelineEventRequest"/> handed to the host that does, through the pipeline event client (FDW-3773).
/// </summary>
/// <remarks>
/// A delivery that fails is logged with a MessageLogging code and the returned task still completes: the
/// interface returns a plain task, and a run must not fail because a browser could not be told.
/// </remarks>
public sealed class RelayPipelineStatusBroadcaster : IPipelineStatusBroadcaster
{
    private readonly IPipelineEventClient _client;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="RelayPipelineStatusBroadcaster"/> class.</summary>
    public RelayPipelineStatusBroadcaster(IPipelineEventClient client, ILogger<RelayPipelineStatusBroadcaster>? logger = null)
    {
        _client = client;
        _logger = logger ?? NullLogger<RelayPipelineStatusBroadcaster>.Instance;
    }

    /// <inheritdoc />
    public Task BroadcastStatusChange(string pipelineName, Guid executionId, string status, string? message = null, Guid? orgId = null)
        => Send(new PipelineEventRequest
        {
            Kind = PipelineEventKinds.StatusChanged.Name,
            OrgId = orgId,
            Status = new PipelineStatusUpdate { PipelineName = pipelineName, ExecutionId = executionId, Status = status, Message = message },
        });

    /// <inheritdoc />
    public Task BroadcastProgress(string pipelineName, Guid executionId, int recordsExtracted, int recordsTransformed, int recordsLoaded, int recordsFailed, int progressPercentage, Guid? orgId = null)
        => Send(new PipelineEventRequest
        {
            Kind = PipelineEventKinds.Progress.Name,
            OrgId = orgId,
            Progress = new PipelineProgressUpdate
            {
                PipelineName = pipelineName, ExecutionId = executionId, RecordsExtracted = recordsExtracted,
                RecordsTransformed = recordsTransformed, RecordsLoaded = recordsLoaded, RecordsFailed = recordsFailed,
                ProgressPercentage = progressPercentage,
            },
        });

    /// <inheritdoc />
    public Task BroadcastCompletion(PipelineExecutionComplete completion, Guid? orgId = null)
        => Send(new PipelineEventRequest { Kind = PipelineEventKinds.Completed.Name, OrgId = orgId, Completion = completion });

    /// <inheritdoc />
    public Task BroadcastTaskStatus(Guid executionId, Guid taskId, string status, long recordsIn, long recordsOut, long recordsDiscarded, long recordsHeld, bool sampleBufferAtCapacity)
        => Send(new PipelineEventRequest
        {
            Kind = PipelineEventKinds.TaskStatus.Name,
            Task = new PipelineTaskStatusUpdate
            {
                ExecutionId = executionId, TaskId = taskId, Status = status, RecordsIn = recordsIn, RecordsOut = recordsOut,
                RecordsDiscarded = recordsDiscarded, RecordsHeld = recordsHeld, SampleBufferAtCapacity = sampleBufferAtCapacity,
            },
        });

    /// <inheritdoc />
    public Task BroadcastEdgeFlow(Guid executionId, Guid sourceTaskId, Guid targetTaskId, long recordsFlowed)
        => Send(new PipelineEventRequest
        {
            Kind = PipelineEventKinds.EdgeFlow.Name,
            EdgeFlow = new PipelineEdgeFlowUpdate { ExecutionId = executionId, SourceTaskId = sourceTaskId, TargetTaskId = targetTaskId, RecordsFlowed = recordsFlowed },
        });

    /// <inheritdoc />
    public Task BroadcastComponents(PipelineComponentsUpdate update, Guid? orgId = null)
        => Send(new PipelineEventRequest { Kind = PipelineEventKinds.ComponentsChanged.Name, OrgId = orgId, Components = update });

    /// <inheritdoc />
    public Task BroadcastExecutionPaused(Guid executionId)
        => Send(new PipelineEventRequest { Kind = PipelineEventKinds.Paused.Name, ExecutionId = executionId });

    /// <inheritdoc />
    public Task BroadcastExecutionResumed(Guid executionId)
        => Send(new PipelineEventRequest { Kind = PipelineEventKinds.Resumed.Name, ExecutionId = executionId });

    private async Task Send(PipelineEventRequest request)
    {
        try
        {
            var result = await _client.Relay(request).ConfigureAwait(false);
            if (result.IsFailure)
            {
                PipelineEventRelayLog.RelayFailed(_logger, request.Kind);
            }
        }
        catch (Exception exception)
        {
            PipelineEventRelayLog.RelayException(_logger, exception, request.Kind);
        }
    }
}
