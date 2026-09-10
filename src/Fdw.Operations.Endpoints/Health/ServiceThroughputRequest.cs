namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Request model for throughput data with a time window.
/// </summary>
public class ServiceThroughputRequest
{
    /// <summary>Gets or sets the service name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the time window (e.g. "5m", "1h"). Defaults to "5m".</summary>
    public string Window { get; set; } = "5m";
}
