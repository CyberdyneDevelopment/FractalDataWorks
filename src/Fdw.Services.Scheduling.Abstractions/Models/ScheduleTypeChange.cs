using System;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Abstractions.Models;

/// <summary>
/// Judges a scheduler type a caller states on an update (FDW-1171), in one place for every host that takes one.
/// </summary>
/// <remarks>
/// Why a check and not an apply: the type of a stored schedule is the implementation its row is saved under, so an update cannot
/// change it without moving the row. A type that is stated and equals the stored one changes nothing and is accepted; any other
/// is refused with the code that names both types, so a stated member is never accepted and then dropped.
/// </remarks>
public static class ScheduleTypeChange
{
    /// <summary>Checks a stated scheduler type against the stored schedule's.</summary>
    /// <param name="statedType">The type the caller stated, or <see langword="null"/> when the request does not state one.</param>
    /// <param name="storedType">The type the schedule is stored under.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success when no type is stated or it names the stored type; otherwise a 400-class refusal naming both.</returns>
    public static IGenericResult Check(string? statedType, string storedType, ILogger logger)
    {
        if (statedType is null || string.Equals(statedType, storedType, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult.Success();
        }

        return GenericResult.Failure(SchedulingResultCodes.ByName("ScheduleTypeChangeRefused"), logger,
            ResultDetails.Create("StatedType", statedType, "StoredType", storedType));
    }
}
