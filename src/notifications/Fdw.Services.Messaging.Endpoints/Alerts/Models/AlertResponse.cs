using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// What an alert allows the caller and what has been done to it (FDW-2420).
/// </summary>
public class AlertResponse
{
    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the alert's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets a value indicating whether the caller's screen shows the alert now: no action that hides it is in force.</summary>
    public bool Shown { get; set; }

    /// <summary>Gets or sets when the alert is shown again, or null when no action hides it for a time.</summary>
    public DateTimeOffset? ReturnsAt { get; set; }

    /// <summary>Gets or sets what has been done to the alert and still stands: the caller's own actions and those taken for everyone.</summary>
    public IReadOnlyList<AlertTakenResponse> Taken { get; set; } = [];

    /// <summary>Gets or sets the actions the caller may take on the alert now, in the order the kind offers them.</summary>
    public IReadOnlyList<AlertOfferResponse> Actions { get; set; } = [];

    /// <summary>
    /// Makes the response for an alert.
    /// </summary>
    /// <param name="summary">What the service answered.</param>
    /// <returns>The response.</returns>
    public static AlertResponse Of(AlertSummary summary) => new()
    {
        Kind = summary.Kind,
        Id = summary.Id,
        Shown = summary.Shown,
        ReturnsAt = summary.ReturnsAt,
        Taken = [.. summary.Taken.Select(AlertTakenResponse.Of)],
        Actions = [.. summary.Actions.Select(AlertOfferResponse.Of)]
    };
}
