using System;

namespace Fdw.Services.Pipelines.Hubs;

/// <summary>
/// Per-task status and counter update broadcast to execution group members.
/// </summary>
public sealed class PipelineTaskStatusUpdate
{
    /// <summary>Gets or sets the execution ID.</summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the task node ID.</summary>
    public Guid TaskId { get; set; }

    /// <summary>Gets or sets the task status (e.g., "Running", "Complete", "Failed").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets the total records received by this task.</summary>
    public long RecordsIn { get; set; }

    /// <summary>Gets or sets the total records emitted on the data stream.</summary>
    public long RecordsOut { get; set; }

    /// <summary>Gets or sets the total records routed to the reject/error stream.</summary>
    public long RecordsDiscarded { get; set; }

    /// <summary>Gets or sets the count of records currently held in the task's processing window.</summary>
    public long RecordsHeld { get; set; }

    /// <summary>Gets or sets whether the sample ring buffer has hit the byte cap.</summary>
    public bool SampleBufferAtCapacity { get; set; }

    /// <summary>Gets or sets the broadcast timestamp.</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
