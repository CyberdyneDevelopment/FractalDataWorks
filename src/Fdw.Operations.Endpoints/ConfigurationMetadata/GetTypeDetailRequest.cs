namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Request for getting configuration type detail.
/// </summary>
public class GetTypeDetailRequest
{
    /// <summary>
    /// Gets or sets the category (from route or query string).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service type name (from route or query string).
    /// </summary>
    public string Type { get; set; } = string.Empty;
}
