using System;
using Fdw.Configuration;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// The contract every Schedule implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be
/// registered against this domain or handed back by a read of it.
/// <para>
/// It also carries <see cref="IScheduleDefinition"/> — what a schedule IS, independent of which
/// trigger kind it is — so a caller handed the implementation by a domain read can ask the questions
/// a schedule answers without knowing the kind.
/// </para>
/// <para>
/// Why every trigger kind's fields sit on one contract: <c>sched.Schedule</c> is still one table
/// carrying all of them. When the domain row sheds its payload, the per-kind fields go with it and
/// this contract keeps only what every kind shares.
/// </para>
/// </remarks>
public interface IScheduleImplementationConfiguration : IImplementationConfiguration, IScheduleDefinition
{
    /// <summary>Gets or sets the schedule's description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the cron expression, when the trigger kind is Cron.</summary>
    string? CronExpression { get; set; }

    /// <summary>Gets or sets the interval in seconds, when the trigger kind is Interval.</summary>
    int? IntervalSeconds { get; set; }

    /// <summary>Gets or sets the instant to run at, when the trigger kind is Once.</summary>
    DateTimeOffset? OneTimeDateTime { get; set; }

    /// <summary>Gets or sets the event that fires the schedule, when the trigger kind is event-driven.</summary>
    string? EventName { get; set; }

    /// <summary>Gets or sets the IANA time zone the schedule is evaluated in.</summary>
    string TimeZoneId { get; set; }

    /// <summary>Gets or sets how many times a failed run is retried.</summary>
    int MaxRetries { get; set; }

    /// <summary>Gets or sets the delay between retries, in seconds.</summary>
    int RetryDelaySeconds { get; set; }

    /// <summary>Gets or sets how long a run may take before it is abandoned, in seconds.</summary>
    int TimeoutSeconds { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system (if migrated).</summary>
    DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets the timestamp when the record was created.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets the database user who created the record.</summary>
    string CreateBy { get; set; }

    /// <summary>Gets the application user on whose behalf the record was created.</summary>
    string CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    string ModifyBy { get; set; }

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    string ModifyOnBehalfOf { get; set; }
}
