using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a scheduled job has completed successfully.
/// </summary>
[Message("JobCompleted")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class JobCompletedMessage : SchedulingMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobCompletedMessage"/> class.
    /// </summary>
    public JobCompletedMessage()
        : base(3003, "JobCompleted", MessageSeverity.Information,
               "Scheduled job completed successfully", "SCHED_JOB_COMPLETED")
    { }

    /// <summary>
    /// Initializes a new instance with the job identifier and duration.
    /// </summary>
    /// <param name="jobId">The identifier of the completed job.</param>
    /// <param name="durationMs">The duration of the job execution in milliseconds.</param>
    public JobCompletedMessage(string jobId, long durationMs)
        : base(3003, "JobCompleted", MessageSeverity.Information,
               $"Job '{jobId}' completed successfully in {durationMs}ms", "SCHED_JOB_COMPLETED")
    { }
}