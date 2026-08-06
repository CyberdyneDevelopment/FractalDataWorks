namespace Fdw.UI.Components.Services;

using System;
using System.Globalization;

/// <summary>
/// Formats <see cref="DateTimeOffset"/> values as human-readable relative time strings.
/// </summary>
public static class RelativeTimeFormatter
{
    /// <summary>
    /// Formats a <see cref="DateTimeOffset"/> as a relative time string (e.g., "5 min ago").
    /// </summary>
    /// <param name="timestamp">The timestamp to format.</param>
    /// <returns>A human-readable relative time string.</returns>
    public static string Format(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue)
        {
            return "Never";
        }

        return Format(timestamp.Value);
    }

    /// <summary>
    /// Formats a <see cref="DateTimeOffset"/> as a relative time string (e.g., "5 min ago").
    /// </summary>
    /// <param name="timestamp">The timestamp to format.</param>
    /// <returns>A human-readable relative time string.</returns>
    public static string Format(DateTimeOffset timestamp)
    {
        var elapsed = DateTimeOffset.UtcNow - timestamp;

        if (elapsed.TotalSeconds < 0)
        {
            return "Just now";
        }

        if (elapsed.TotalSeconds < 60)
        {
            return "Just now";
        }

        if (elapsed.TotalMinutes < 2)
        {
            return "1 min ago";
        }

        if (elapsed.TotalMinutes < 60)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} min ago", (int)elapsed.TotalMinutes);
        }

        if (elapsed.TotalHours < 2)
        {
            return "1 hour ago";
        }

        if (elapsed.TotalHours < 24)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} hours ago", (int)elapsed.TotalHours);
        }

        if (elapsed.TotalDays < 2)
        {
            return "Yesterday";
        }

        if (elapsed.TotalDays < 7)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} days ago", (int)elapsed.TotalDays);
        }

        if (elapsed.TotalDays < 30)
        {
            int weeks = (int)(elapsed.TotalDays / 7);
            return weeks == 1 ? "1 week ago" : string.Format(CultureInfo.InvariantCulture, "{0} weeks ago", weeks);
        }

        if (elapsed.TotalDays < 365)
        {
            int months = (int)(elapsed.TotalDays / 30);
            return months == 1 ? "1 month ago" : string.Format(CultureInfo.InvariantCulture, "{0} months ago", months);
        }

        int years = (int)(elapsed.TotalDays / 365);
        return years == 1 ? "1 year ago" : string.Format(CultureInfo.InvariantCulture, "{0} years ago", years);
    }
}
