namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// The request to enable a disabled connection again (FDW-3951): the connection by name in the route, and why in the body.
/// </summary>
public class EnableConnectionRequest : ConnectionNameRequest
{
    /// <summary>Gets or sets why the connection is enabled again; recorded with the state and required.</summary>
    public string Reason { get; set; } = string.Empty;
}
