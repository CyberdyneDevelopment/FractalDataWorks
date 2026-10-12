using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a job has been successfully scheduled.
/// </summary>
[Message("JobScheduled")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class JobScheduledMessage : SchedulingMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduledMessage"/> class.
    /// </summary>
    public JobScheduledMessage()
        : base(3001, "JobScheduled", MessageSeverity.Information,
               "Job has been scheduled successfully", "SCHED_JOB_SCHEDULED")
    { }

    /// <summary>
    /// Initializes a new instance with the job identifier.
    /// </summary>
    /// <param name="jobId">The identifier of the scheduled job.</param>
    public JobScheduledMessage(string jobId)
        : base(3001, "JobScheduled", MessageSeverity.Information,
               $"Job '{jobId}' has been scheduled successfully", "SCHED_JOB_SCHEDULED")
    { }
}