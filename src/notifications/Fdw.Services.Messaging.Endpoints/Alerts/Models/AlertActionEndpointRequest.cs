using System;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A request to take an action on one alert, or to take it back (FDW-2420). The alert and the action are in the route; the choice, for an action that offers
/// lengths of time, is in the body.
/// </summary>
public class AlertActionEndpointRequest
{
    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the alert's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the action's name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the length of time, for an action that offers lengths; otherwise leave it out. The server makes the time; a caller never sends one.</summary>
    public string? Choice { get; set; }
}
