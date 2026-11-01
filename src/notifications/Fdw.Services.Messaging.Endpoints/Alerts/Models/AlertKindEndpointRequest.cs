namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A request that names one kind of alert in the route (FDW-2430).
/// </summary>
public class AlertKindEndpointRequest
{
    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;
}
