using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Execution.Abstractions.OptionTypes;

namespace Fdw.Services.Scheduling.Execution;

/// <summary>
/// Database-backed record of a schedule execution.
/// Tracks when schedules fire and the outcome of their triggered jobs.
/// </summary>
/// <remarks>
/// <para>
/// This complements PipelineExecutionRecord (in Fdw.Services.Etl)
/// by tracking the schedule-level view of executions.
/// </para>
/// <para>
/// One schedule execution may trigger one pipeline execution, and they can be
/// correlated via ScheduleName/PipelineName and timing.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class ScheduleExecutionRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for this execution record.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name for display/query purposes.
    /// Format: "{ScheduleName}_{TriggeredAt:yyyyMMddHHmmss}"
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name (not used for execution records).
    /// </summary>
    public string SectionName => $"ScheduleExecutions:{Id}";

    /// <summary>
    /// Gets or sets the name of the schedule that fired.
    /// </summary>
    public string ScheduleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schedule type (e.g., "Cron", "Interval").
    /// </summary>
    public string? ScheduleType { get; set; }

    /// <summary>
    /// Gets or sets the name of the pipeline that was triggered.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trigger type that caused this execution.
    /// </summary>
    /// <value>One of: "Scheduled", "Manual", "Retry", "Catchup"</value>
    public string TriggerType { get; set; } = "Scheduled";

    /// <summary>
    /// Gets or sets the execution status.
    /// </summary>
    /// <value>One of: "Triggered", "Running", "Succeeded", "Failed", "Skipped", "Cancelled"</value>
    [ValuesFrom(typeof(ProcessStates))]
    public string Status { get; set; } = "Triggered";

    /// <summary>
    /// Gets or sets when the schedule was triggered.
    /// </summary>
    public DateTimeOffset TriggeredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the scheduled fire time (may differ from TriggeredAt due to delays).
    /// </summary>
    public DateTimeOffset? ScheduledFireTime { get; set; }

    /// <summary>
    /// Gets or sets when execution started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the execution duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the next scheduled fire time after this execution.
    /// </summary>
    public DateTimeOffset? NextFireTime { get; set; }

    /// <summary>
    /// Gets or sets the retry attempt number (0 for first attempt).
    /// </summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// Gets or sets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the hostname/instance that processed this schedule.
    /// </summary>
    public string? ExecutedBy { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID linking to pipeline execution.
    /// </summary>
    public string? CorrelationId { get; set; }
}
