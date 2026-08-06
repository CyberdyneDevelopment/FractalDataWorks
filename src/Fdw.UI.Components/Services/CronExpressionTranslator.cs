namespace Fdw.UI.Components.Services;

using System;
using System.Globalization;
using Fdw.Conventions;

/// <summary>
/// Translates cron expressions into human-readable descriptions.
/// </summary>
public static class CronExpressionTranslator
{
    /// <summary>
    /// Translates a cron expression (5-field or 6-field) to a human-readable string.
    /// </summary>
    /// <param name="cronExpression">The cron expression to translate.</param>
    /// <returns>A human-readable description, or the original expression if it cannot be parsed.</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 35)] // Sequential cron pattern matching — each branch is an independent pattern check
    public static string Translate(string? cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return "No schedule";
        }

        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Support 5-field (standard) and 6-field (with seconds) cron
        if (parts.Length < 5 || parts.Length > 6)
        {
            return cronExpression;
        }

        int offset = parts.Length == 6 ? 1 : 0;
        string minute = parts[offset];
        string hour = parts[offset + 1];
        string dayOfMonth = parts[offset + 2];
        string month = parts[offset + 3];
        string dayOfWeek = parts[offset + 4];

        // Every minute
        if (string.Equals(minute, "*", StringComparison.Ordinal) &&
            string.Equals(hour, "*", StringComparison.Ordinal) &&
            string.Equals(dayOfMonth, "*", StringComparison.Ordinal) &&
            string.Equals(dayOfWeek, "*", StringComparison.Ordinal))
        {
            return "Every minute";
        }

        // Every N minutes
        if (minute.StartsWith("*/", StringComparison.Ordinal) &&
            string.Equals(hour, "*", StringComparison.Ordinal) &&
            string.Equals(dayOfMonth, "*", StringComparison.Ordinal) &&
            string.Equals(dayOfWeek, "*", StringComparison.Ordinal))
        {
            string interval = minute.Substring(2);
            return string.Concat("Every ", interval, " minutes");
        }

        // Every N hours
        if (string.Equals(minute, "0", StringComparison.Ordinal) &&
            hour.StartsWith("*/", StringComparison.Ordinal) &&
            string.Equals(dayOfMonth, "*", StringComparison.Ordinal) &&
            string.Equals(dayOfWeek, "*", StringComparison.Ordinal))
        {
            string interval = hour.Substring(2);
            return string.Concat("Every ", interval, " hours");
        }

        // Specific time daily
        if (int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out int min) &&
            int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out int hr) &&
            string.Equals(dayOfMonth, "*", StringComparison.Ordinal) &&
            string.Equals(dayOfWeek, "*", StringComparison.Ordinal))
        {
            return string.Format(CultureInfo.InvariantCulture, "Daily at {0:D2}:{1:D2}", hr, min);
        }

        // Specific time on weekdays
        if (int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out int wMin) &&
            int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out int wHr) &&
            string.Equals(dayOfMonth, "*", StringComparison.Ordinal) &&
            string.Equals(dayOfWeek, "1-5", StringComparison.Ordinal))
        {
            return string.Format(CultureInfo.InvariantCulture, "Weekdays at {0:D2}:{1:D2}", wHr, wMin);
        }

        // Weekly on specific day
        if (int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weekMin) &&
            int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weekHr) &&
            string.Equals(dayOfMonth, "*", StringComparison.Ordinal) &&
            int.TryParse(dayOfWeek, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dow))
        {
            string dayName = GetDayName(dow);
            return string.Format(CultureInfo.InvariantCulture, "Weekly on {0} at {1:D2}:{2:D2}", dayName, weekHr, weekMin);
        }

        // Monthly on specific day
        if (int.TryParse(minute, NumberStyles.Integer, CultureInfo.InvariantCulture, out int monthMin) &&
            int.TryParse(hour, NumberStyles.Integer, CultureInfo.InvariantCulture, out int monthHr) &&
            int.TryParse(dayOfMonth, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dom) &&
            string.Equals(dayOfWeek, "*", StringComparison.Ordinal))
        {
            return string.Format(CultureInfo.InvariantCulture, "Monthly on day {0} at {1:D2}:{2:D2}", dom, monthHr, monthMin);
        }

        return cronExpression;
    }

    private static string GetDayName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            7 => "Sunday",
            _ => dayOfWeek.ToString(CultureInfo.InvariantCulture),
        };
    }
}
