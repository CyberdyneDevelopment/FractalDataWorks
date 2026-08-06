namespace Fdw.UI.Components.Services;

using System;
using System.Globalization;

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
    /// <returns>A formatted duration string.</returns>
    public static string Format(DateTimeOffset start, DateTimeOffset? end)
    {
        if (!end.HasValue)
        {
            return "\u2014"; // em dash
        }

        var duration = end.Value - start;
        return Format(duration);
    }

    /// <summary>
    /// Formats a <see cref="TimeSpan"/> as a human-readable string.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>A formatted duration string.</returns>
    public static string Format(TimeSpan duration)
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
