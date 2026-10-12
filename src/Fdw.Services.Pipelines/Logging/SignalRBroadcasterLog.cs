using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Pipelines.Logging;

/// <summary>
/// MessageLogging for <see cref="Fdw.Services.Pipelines.Notifications.PipelineStatusBroadcaster"/>
/// lifecycle and coalescing operations.
/// EventId range: 9161-9175
/// </summary>
[MessageLoggingTypeCode("PIPELINES")]
public static partial class SignalRBroadcasterLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Startup / Configuration (9161-9163)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged once at startup when the broadcaster is initialized with its configured Hz and byte cap.
    /// </summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "PipelineStatusBroadcaster configured: BroadcastHz={broadcastHz}, SampleBufferMaxBytes={sampleBufferMaxBytes}")]
    public static partial IGenericMessage BroadcasterConfigured(
        ILogger logger,
        int broadcastHz,
        long sampleBufferMaxBytes);

    // ═══════════════════════════════════════════════════════════════════════════
    // Coalescing (9164-9167)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace when a broadcast is coalesced (skipped because the cadence timer
    /// has not fired yet).
    /// </summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Trace,
        Message = "Coalescing task status broadcast for execution {executionId}, task {taskId} (Hz cap)")]
    public static partial IGenericMessage TaskStatusBroadcastCoalesced(
        ILogger logger,
        System.Guid executionId,
        System.Guid taskId);

    /// <summary>
    /// Logged at Trace when an edge-flow broadcast is coalesced.
    /// </summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Trace,
        Message = "Coalescing edge flow broadcast for execution {executionId}, edge {sourceTaskId}->{targetTaskId} (Hz cap)")]
    public static partial IGenericMessage EdgeFlowBroadcastCoalesced(
        ILogger logger,
        System.Guid executionId,
        System.Guid sourceTaskId,
        System.Guid targetTaskId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Terminal broadcasts (9168-9169)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Trace when the terminal task-status broadcast is sent (ignoring cadence).
    /// </summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Trace,
        Message = "Sending terminal task status broadcast for execution {executionId}, task {taskId}")]
    public static partial IGenericMessage TaskStatusTerminalBroadcast(
        ILogger logger,
        System.Guid executionId,
        System.Guid taskId);

    // ═══════════════════════════════════════════════════════════════════════════
    // Org firehose scoping (11010-11011)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Logged at Debug when a lifecycle broadcast is additionally fanned out to the owning org's
    /// firehose group (<c>org:{orgId}:pipeline-updates</c>).
    /// </summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Debug,
        Message = "Targeting org firehose org:{orgId}:pipeline-updates for execution {executionId}")]
    public static partial IGenericMessage OrgFirehoseTargeted(
        ILogger logger,
        System.Guid orgId,
        System.Guid executionId);

    /// <summary>
    /// Logged at Debug when a lifecycle broadcast has no owning org, so no org firehose is targeted —
    /// delivery is to the pipeline/execution groups only (there is no global cross-org firehose).
    /// </summary>
    [MessageLogging(
        EventId = 11011,
        Level = LogLevel.Debug,
        Message = "No owning org for execution {executionId}; no org firehose targeted")]
    public static partial IGenericMessage NoOrgFirehose(
        ILogger logger,
        System.Guid executionId);
}
