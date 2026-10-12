namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// What a Notify stage states, as a request carries it: which notification rule sends, at which point of the run, and what it says.
/// </summary>
public interface INotificationSpec
{
    /// <summary>Gets the name of the notification rule that sends the message; the rule chooses channel and severity.</summary>
    string RuleName { get; }

    /// <summary>Gets the run point the notification fires at: an option name of the RunPoints collection.</summary>
    string RunPoint { get; }

    /// <summary>Gets the message template, which may use the reserved variables of a run in double braces.</summary>
    string Message { get; }
}
