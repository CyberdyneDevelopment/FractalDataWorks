using Fdw.Messages;
using Fdw.Messages.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.Messages;

/// <summary>
/// CurrentMessage indicating that a scheduled job has failed.
/// </summary>
[Message("JobFailed")]
[MessageOption(typeof(SchedulingMessageCollectionBase))]
public sealed class JobFailedMessage : SchedulingMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobFailedMessage"/> class.
    /// </summary>
    public JobFailedMessage()
        : base(3002, "JobFailed", MessageSeverity.Error,
               "Scheduled job execution failed", "SCHED_JOB_FAILED")
    { }

    /// <summary>
    /// Initializes a new instance with the job identifier and error details.
    /// </summary>
    /// <param name="jobId">The identifier of the failed job.</param>
    /// <param name="reason">The reason for the failure.</param>
    public JobFailedMessage(string jobId, string reason)
        : base(3002, "JobFailed", MessageSeverity.Error,
               $"Job '{jobId}' failed: {reason}", "SCHED_JOB_FAILED")
    { }
}