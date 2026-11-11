using System;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Checks the level of thresholds on waiting runs an owner states before it is written (FDW-3161, FDW-3975): its mode is stated, is an option, and is one the owner can
/// state. The thresholds are attached to the owner as threshold types and are checked by their own validators.
/// </summary>
public static class WaitThresholdValidator
{
    /// <summary>Checks a level.</summary>
    /// <param name="ownerKind">Whether the owner is the server, a pipeline or a schedule.</param>
    /// <param name="level">The level as the API states it.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success, or a refusal naming the member and what is wrong with it.</returns>
    public static IGenericResult Check(string ownerKind, WaitThresholdLevelPayload level, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(ownerKind))
            return GenericResult.Failure(WaitThresholdLog.OwnerKindRequired(logger));

        var mode = ModeOf(ownerKind, level, logger);
        return mode.IsFailure ? mode : GenericResult.Success();
    }

    private static IGenericResult<IWaitThresholdMode> ModeOf(string ownerKind, WaitThresholdLevelPayload level, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(level.Mode))
        {
            return GenericResult<IWaitThresholdMode>.Failure(WaitThresholdLog.Refused(logger, "mode", "the level does not state its mode"));
        }

        var mode = WaitThresholdModes.ByName(level.Mode);
        if (ReferenceEquals(mode, WaitThresholdModes.NotFound))
        {
            return GenericResult<IWaitThresholdMode>.Failure(WaitThresholdLog.Refused(logger, "mode", $"'{level.Mode}' is not one of {nameof(WaitThresholdModes)}"));
        }

        return mode.Levels.Contains(ownerKind, StringComparer.Ordinal)
            ? GenericResult<IWaitThresholdMode>.Success(mode)
            : GenericResult<IWaitThresholdMode>.Failure(WaitThresholdLog.Refused(logger, "mode", $"the mode '{mode.Name}' cannot be stated by a {ownerKind}"));
    }
}
