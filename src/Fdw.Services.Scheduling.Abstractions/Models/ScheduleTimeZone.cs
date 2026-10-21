using System;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Abstractions.Models;

/// <summary>
/// Judges a time zone a caller states for a schedule (FDW-1170), in one place for every host that takes one: the API proxy and
/// the scheduler's own create and update endpoints.
/// </summary>
/// <remarks>
/// Why one check: the triggers and the Quartz translators all resolve the stored id with
/// <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>, so a zone is good exactly when that call accepts it. Judging it with
/// any other rule would accept a zone the scheduler later throws on, or refuse one it would have run.
/// </remarks>
public static class ScheduleTimeZone
{
    /// <summary>Checks a stated time zone.</summary>
    /// <param name="timeZoneId">The zone the caller stated, or <see langword="null"/> when the request does not state one.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success when no zone is stated or the stated zone resolves; otherwise a 400-class refusal naming the value.</returns>
    public static IGenericResult Check(string? timeZoneId, ILogger logger)
    {
        if (timeZoneId is null)
        {
            return GenericResult.Success();
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return Refuse(logger, timeZoneId, "it is empty");
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return GenericResult.Success();
        }
        catch (TimeZoneNotFoundException ex)
        {
            return Refuse(logger, timeZoneId, ex.Message);
        }
        catch (InvalidTimeZoneException ex)
        {
            return Refuse(logger, timeZoneId, ex.Message);
        }
    }

    /// <summary>Checks the time zone of a schedule that is being created: it must be stated, and be one the scheduler can resolve (FDW-2059).</summary>
    /// <param name="timeZoneId">The stated zone; <see langword="null"/> when the request left it out.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success, or the coded refusal that names the value (an empty value when none was stated).</returns>
    public static IGenericResult Require(string? timeZoneId, ILogger logger) =>
        timeZoneId is null
            ? Refuse(logger, string.Empty, "the schedule does not state its time zone")
            : Check(timeZoneId, logger);

    private static IGenericResult Refuse(ILogger logger, string timeZoneId, string reason) =>
        GenericResult.Failure(SchedulingResultCodes.ByName("ScheduleTimeZoneInvalid"), logger,
            ResultDetails.Create("TimeZoneId", timeZoneId, "Reason", reason));
}
