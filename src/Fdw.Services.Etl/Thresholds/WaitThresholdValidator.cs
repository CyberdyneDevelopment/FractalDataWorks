using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Etl.Abstractions.Monitoring.AlertSeverityOptions;
using Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Judges a level of thresholds on waiting runs before it is saved (FDW-3161), on what the level itself says: its mode exists and may be stated by its owner, a mode that
/// states thresholds has some and one that does not has none, and every threshold states every member, names a comparison and a severity that exist, and does something
/// (sends a notification rule or stops new runs). What only the data can say (that a notification rule or a pipeline exists) is judged by the store. The first thing wrong is
/// refused, naming the member; nothing is filled in.
/// </summary>
public static class WaitThresholdValidator
{
    /// <summary>Judges a level.</summary>
    /// <param name="ownerKind">The kind of the owner, as <c>WaitThresholdOwners</c> names it.</param>
    /// <param name="level">The level.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the refusal naming the member and why.</returns>
    public static IGenericResult Check(string ownerKind, WaitThresholdLevelPayload level, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerKind);
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(logger);

        var mode = ModeOf(ownerKind, level, logger);
        if (mode.IsFailure || mode.Value is null)
        {
            return mode;
        }

        if (mode.Value.StatesThresholds ? level.Thresholds.Count == 0 : level.Thresholds.Count > 0)
        {
            return Refuse(logger, "thresholds", mode.Value.StatesThresholds
                ? $"the mode '{mode.Value.Name}' states thresholds of its own, and the level states none"
                : $"the mode '{mode.Value.Name}' states no thresholds, and the level lists {level.Thresholds.Count}");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < level.Thresholds.Count; index++)
        {
            var checkedThreshold = One(index, level.Thresholds[index], names, logger);
            if (checkedThreshold.IsFailure)
            {
                return checkedThreshold;
            }
        }

        return GenericResult.Success();
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

    // One threshold: every member stated, the options exist, and it does something.
    private static IGenericResult One(int index, WaitThresholdPayload threshold, HashSet<string> names, ILogger logger)
    {
        var at = $"thresholds[{index}]";
        if (string.IsNullOrWhiteSpace(threshold.Name))
        {
            return Refuse(logger, $"{at}.name", "the threshold does not state its name");
        }

        if (!names.Add(threshold.Name))
        {
            return Refuse(logger, $"{at}.name", $"the level already has a threshold named '{threshold.Name}'");
        }

        var stated = Stated(at, threshold, logger);
        return stated.IsFailure ? stated : Actions(at, threshold, logger);
    }

    private static IGenericResult Stated(string at, WaitThresholdPayload threshold, ILogger logger)
    {
        var options = Options(at, threshold, logger);
        return options.IsFailure ? options : Members(at, threshold, logger);
    }

    // The comparison and the severity name options that exist.
    private static IGenericResult Options(string at, WaitThresholdPayload threshold, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(threshold.Comparison) || ReferenceEquals(ComparisonOperators.ByName(threshold.Comparison), ComparisonOperators.NotFound))
        {
            return Refuse(logger, $"{at}.comparison", $"'{threshold.Comparison}' is not one of {nameof(ComparisonOperators)}");
        }

        if (string.IsNullOrWhiteSpace(threshold.Severity) || ReferenceEquals(AlertSeverities.ByName(threshold.Severity), AlertSeverities.NotFound))
        {
            return Refuse(logger, $"{at}.severity", $"'{threshold.Severity}' is not one of {nameof(AlertSeverities)}");
        }

        return GenericResult.Success();
    }

    // The number, the pipeline counted and the two switches are stated.
    private static IGenericResult Members(string at, WaitThresholdPayload threshold, ILogger logger)
    {
        if (threshold.Number is not >= 0)
        {
            return Refuse(logger, $"{at}.number", "the threshold does not state a number of 0 or more");
        }

        if (threshold.CountedPipeline is not null && string.IsNullOrWhiteSpace(threshold.CountedPipeline))
        {
            return Refuse(logger, $"{at}.countedPipeline", "the pipeline counted is blank; state a pipeline, or none to count the whole server's waiting runs");
        }

        if (threshold.StopsNewRuns is null || threshold.IsEnabled is null)
        {
            return Refuse(logger, threshold.StopsNewRuns is null ? $"{at}.stopsNewRuns" : $"{at}.isEnabled", "the threshold does not state it");
        }

        return GenericResult.Success();
    }

    // What the threshold does: it sends notification rules, stops new runs, or both; one that does neither only exists.
    private static IGenericResult Actions(string at, WaitThresholdPayload threshold, ILogger logger)
    {
        if (threshold.NotificationRules.Any(string.IsNullOrWhiteSpace))
        {
            return Refuse(logger, $"{at}.notificationRules", "a notification rule is named by a blank name");
        }

        if (threshold.NotificationRules.Distinct(StringComparer.OrdinalIgnoreCase).Count() != threshold.NotificationRules.Count)
        {
            return Refuse(logger, $"{at}.notificationRules", "a notification rule is named twice");
        }

        return threshold.NotificationRules.Count == 0 && threshold.StopsNewRuns != true
            ? Refuse(logger, at, "the threshold does nothing: it sends no notification rule and does not stop new runs")
            : GenericResult.Success();
    }

    private static IGenericResult Refuse(ILogger logger, string member, string reason) => GenericResult.Failure(WaitThresholdLog.Refused(logger, member, reason));
}
