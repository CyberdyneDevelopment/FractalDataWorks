namespace Fdw.UI.Components.Services;

using System;
using System.Globalization;
using Fdw.UI.Components.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Formats durations as human-readable strings.
/// </summary>
public static class DurationFormatter
{
    /// <summary>
    /// Formats a duration from start and end timestamps.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <param name="end">The end time, or null if not completed.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="NullLogger.Instance"/> when not supplied.</param>
    /// <returns>A formatted duration string.</returns>
    // Why: NullLogger fallback is the only acceptable ?? fallback per FDW conventions; this static
    // helper has no DI-constructed instance to hold a logger, so it is threaded through as an
    // optional trailing parameter instead, mirroring the EntityPicker/ObjectPicker component pattern.
    public static string Format(DateTimeOffset start, DateTimeOffset? end, ILogger? logger = null)
    {
        if (!end.HasValue)
        {
            DurationFormatterLog.FormattingIncompleteRange(logger ?? NullLogger.Instance, start);
            return "\u2014"; // em dash
        }

        DurationFormatterLog.FormattingRange(logger ?? NullLogger.Instance, start, end.Value);
        var duration = end.Value - start;
        return Format(duration, logger);
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> as a human-readable string.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <param name="logger">Optional logger. Falls back to <see cref="NullLogger.Instance"/> when not supplied.</param>
    /// <returns>A formatted duration string.</returns>
    public static string Format(TimeSpan duration, ILogger? logger = null)
    {
        var effectiveLogger = logger ?? NullLogger.Instance;
        DurationFormatterLog.FormattingDuration(effectiveLogger, duration);

        var result = ResolveDurationString(duration);

        DurationFormatterLog.FormattedDuration(effectiveLogger, duration, result);
        return result;
    }

    private static string ResolveDurationString(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1000)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F0}ms", duration.TotalMilliseconds);
        }

        if (duration.TotalSeconds < 60)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F1}s", duration.TotalSeconds);
        }

        if (duration.TotalMinutes < 60)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:F1}m", duration.TotalMinutes);
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:F1}h", duration.TotalHours);
    }
}
