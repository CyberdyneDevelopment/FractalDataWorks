namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// Request model for health history with a time window.
/// </summary>
public class ServiceHealthHistoryRequest
{
    /// <summary>Gets or sets the service name (from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the time window (e.g. "1h", "24h"). Defaults to "1h".</summary>
    public string Window { get; set; } = "1h";
}
