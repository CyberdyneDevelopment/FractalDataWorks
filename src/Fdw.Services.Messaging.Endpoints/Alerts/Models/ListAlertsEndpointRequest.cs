using System.Collections.Generic;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Query of what some alerts allow the caller (FDW-2420).
/// </summary>
public class ListAlertsEndpointRequest
{
    /// <summary>Gets or sets the alerts asked about, each written <c>kind:id</c>, such as <c>UiFeedback:2d1c...</c>; repeat the parameter for several, at least one and at most 100.</summary>
    public List<string>? Alert { get; set; }
}
