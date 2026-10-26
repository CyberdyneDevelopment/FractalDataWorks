using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// An action that has been taken on an alert and still stands (FDW-2420).
/// </summary>
public class AlertTakenResponse
{
    /// <summary>Gets or sets the action's name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether it was taken for everyone; otherwise it is the caller's own.</summary>
    public bool ForEveryone { get; set; }

    /// <summary>Gets or sets when it was taken.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Gets or sets the name of the person who took it, for an action taken for everyone; otherwise null.</summary>
    public string? ByName { get; set; }

    /// <summary>Gets or sets when it ends, or null when it does not.</summary>
    public DateTimeOffset? Until { get; set; }

    /// <summary>
    /// Makes the response for an action taken.
    /// </summary>
    /// <param name="taken">What the service answered.</param>
    /// <returns>The response.</returns>
    public static AlertTakenResponse Of(AlertActionTaken taken) => new()
    {
        Action = taken.Action,
        ForEveryone = taken.ForEveryone,
        At = taken.At,
        ByName = taken.ByName,
        Until = taken.Until
    };
}
