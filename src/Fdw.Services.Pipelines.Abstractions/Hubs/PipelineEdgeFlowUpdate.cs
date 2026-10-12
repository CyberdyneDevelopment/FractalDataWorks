using System;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// Per-edge record-flow update broadcast to execution group members.
/// </summary>
public sealed class PipelineEdgeFlowUpdate
{
    /// <summary>Gets or sets the execution ID.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the source task ID for this edge.</summary>
    public Guid SourceTaskId { get; set; }

    /// <summary>Gets or sets the target task ID for this edge.</summary>
    public Guid TargetTaskId { get; set; }

    /// <summary>Gets or sets the total records that have flowed across this edge.</summary>
    public long RecordsFlowed { get; set; }

    /// <summary>Gets or sets the broadcast timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
