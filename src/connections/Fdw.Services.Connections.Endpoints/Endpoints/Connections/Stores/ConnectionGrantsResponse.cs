namespace Fdw.Services.Connections.Endpoints;

/// <summary>The connections granted to a tenant (FDW-1581).</summary>
public class ConnectionGrantsResponse
{
    /// <summary>Gets or sets the grants.</summary>
    public System.Collections.Generic.IList<ConnectionGrantResponse> Grants { get; set; } = [];
}
