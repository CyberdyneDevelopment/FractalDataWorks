using System;

namespace Fdw.Services.Messaging;

/// <summary>
/// An action that has been taken on an alert (FDW-2420), by the caller or for everyone.
/// </summary>
public sealed class AlertActionTaken
{
    /// <summary>Gets or sets the action's name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether it was taken for everyone; otherwise it is the caller's own.</summary>
    public bool ForEveryone { get; set; }

    /// <summary>Gets or sets when it was taken.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Gets or sets the person's name as it was then, for an action taken for everyone; <see langword="null"/> for the caller's own.</summary>
    public string? ByName { get; set; }

    /// <summary>Gets or sets when it ends, or <see langword="null"/> when it does not.</summary>
    public DateTimeOffset? Until { get; set; }
}
