using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions.Results;

/// <summary>
/// A schedule's time zone is not one this server can resolve (FDW-1170).
/// </summary>
/// <remarks>
/// 2xxxx (Validation): the zone is the caller's input, so the answer is a 400 that names the value they sent. The check is the
/// one the cron, interval and window triggers make (<see cref="System.TimeZoneInfo.FindSystemTimeZoneById(string)"/>), so a zone
/// accepted here is a zone the scheduler can evaluate the schedule in.
/// </remarks>
[TypeOption(typeof(SchedulingResultCodes), "ScheduleTimeZoneInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ScheduleTimeZoneInvalidCode : SchedulingResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleTimeZoneInvalidCode"/> class.</summary>
    public ScheduleTimeZoneInvalidCode()
        : base(21006, "ScheduleTimeZoneInvalid",
            ResultSeverities.ByName("Error"),
            "timeZoneId '{TimeZoneId}' is not a time zone this server knows ({Reason}); name an IANA zone such as America/Chicago",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller which value was refused and why: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
