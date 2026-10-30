using System;
using System.Collections.Generic;

namespace Fdw.Services.Notifications;

/// <summary>
/// The severities a notification rule may carry, each with the priority a notification sent through the rule has (FDW-2931).
/// </summary>
/// <remarks>
/// One collection read by the rule's save, by the route that serves the choices, and by delivery, so the three cannot disagree about
/// what a severity means.
/// </remarks>
public static class NotificationRuleSeverities
{
    private static readonly IReadOnlyList<KeyValuePair<string, string>> Offered =
    [
        new("Low", "Low"),
        new("Info", "Normal"),
        new("Information", "Normal"),
        new("Normal", "Normal"),
        new("Warning", "High"),
        new("Warn", "High"),
        new("High", "High"),
        new("Error", "Critical"),
        new("Critical", "Critical"),
        new("Fatal", "Critical"),
        new("Severe", "Critical"),
    ];

    /// <summary>Lists each severity a rule may carry with the priority a notification sent through it has, in the order they are offered.</summary>
    /// <returns>The severities and their priorities.</returns>
    public static IReadOnlyList<KeyValuePair<string, string>> All() => Offered;

    /// <summary>Says whether a severity is one a rule may carry.</summary>
    /// <param name="severity">The severity as a rule states it.</param>
    /// <returns>True when it is offered, regardless of case.</returns>
    public static bool Offers(string? severity) => TryPriority(severity, out _);

    /// <summary>Reads the priority a severity maps to.</summary>
    /// <param name="severity">The severity as a rule states it.</param>
    /// <param name="priorityName">The name of the priority, when the severity is offered.</param>
    /// <returns>True when the severity is offered.</returns>
    public static bool TryPriority(string? severity, out string priorityName)
    {
        foreach (var pair in Offered)
        {
            if (string.Equals(pair.Key, severity, StringComparison.OrdinalIgnoreCase))
            {
                priorityName = pair.Value;
                return true;
            }
        }

        priorityName = string.Empty;
        return false;
    }
}
