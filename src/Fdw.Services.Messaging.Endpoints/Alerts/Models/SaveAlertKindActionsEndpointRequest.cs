using System.Collections.Generic;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A request to choose the actions a kind of alert offers the caller's company (FDW-2430). The kind is in the route; the list is in the body.
/// </summary>
public class SaveAlertKindActionsEndpointRequest
{
    /// <summary>Gets or sets the kind of alert.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the names of the actions to offer, in order; required, and an empty list is a choice: the kind then offers nothing.</summary>
    public IReadOnlyList<string>? Actions { get; set; }
}
