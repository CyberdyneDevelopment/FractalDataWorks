using System;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Data;

namespace Fdw.Operations.Escalation;

/// <summary>
/// Words what one level of a policy sends for an alert (FDW-3863).
/// </summary>
/// <remarks>
/// A level may state a message template. It is sent as the body with these tokens replaced: {Subject}, {Message}, {Level}, {Policy}, {SubjectKind}, {SubjectName} and {Threshold}.
/// A template with none of them is sent as it is. A level that states no template sends the alert's own message.
/// </remarks>
internal static class EscalationMessage
{
    /// <summary>Gets the subject a level's notification carries.</summary>
    /// <param name="alert">The alert.</param>
    /// <param name="level">The level's number.</param>
    /// <returns>The alert's subject, led by the level.</returns>
    public static string SubjectOf(EscalationAlertRow alert, int level)
    {
        ArgumentNullException.ThrowIfNull(alert);
        return $"Level {level}: {alert.Subject}";
    }

    /// <summary>Gets the body a level's notification carries.</summary>
    /// <param name="alert">The alert.</param>
    /// <param name="level">The level the policy states.</param>
    /// <returns>The level's template with its tokens replaced, or the alert's message when the level states none.</returns>
    public static string BodyOf(EscalationAlertRow alert, IEscalationLevel level)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ArgumentNullException.ThrowIfNull(level);
        return level.MessageTemplate is not { Length: > 0 } template
            ? alert.Message
            : template
                .Replace("{Subject}", alert.Subject, StringComparison.Ordinal)
                .Replace("{Message}", alert.Message, StringComparison.Ordinal)
                .Replace("{Level}", level.Level.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{Policy}", alert.PolicyName, StringComparison.Ordinal)
                .Replace("{SubjectKind}", alert.SubjectKind, StringComparison.Ordinal)
                .Replace("{SubjectName}", alert.SubjectName, StringComparison.Ordinal)
                .Replace("{Threshold}", alert.ThresholdName, StringComparison.Ordinal);
    }
}
