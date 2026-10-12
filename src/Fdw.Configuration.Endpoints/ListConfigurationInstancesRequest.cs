namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Request for listing configuration instances.
/// </summary>
public sealed class ListConfigurationInstancesRequest
{
    /// <summary>Gets or sets the optional category filter.</summary>
    public string? Category { get; set; }
}
