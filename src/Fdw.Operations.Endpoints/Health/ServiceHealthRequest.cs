namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Request model for service-specific health endpoints.
/// </summary>
public class ServiceHealthRequest
{
    /// <summary>Gets or sets the service name (from route).</summary>
    public string Name { get; set; } = string.Empty;
}
