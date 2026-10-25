using System;
using System.Collections.Generic;

namespace Fdw.Services.Messaging;

/// <summary>
/// What an alert allows the caller and what has been done to it (FDW-2420).
/// </summary>
public sealed class AlertSummary
{
    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the alert's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets a value indicating whether the caller's screen shows the alert now: no action that hides it is in force.</summary>
    public bool Shown { get; set; }

    /// <summary>Gets or sets when the alert is shown again, the latest time of the actions that hide it and end; <see langword="null"/> when none does.</summary>
    public DateTimeOffset? ReturnsAt { get; set; }

    /// <summary>Gets or sets what has been done to the alert and still stands: the caller's own actions, and those taken for everyone.</summary>
    public IReadOnlyList<AlertActionTaken> Taken { get; set; } = [];

    /// <summary>Gets or sets the actions the caller may take on the alert now, in the order the kind offers them.</summary>
    public IReadOnlyList<AlertActionOffer> Actions { get; set; } = [];
}
