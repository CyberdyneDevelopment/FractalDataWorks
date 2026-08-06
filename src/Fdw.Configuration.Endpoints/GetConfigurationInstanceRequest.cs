namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Request for getting a specific configuration instance.
/// </summary>
public sealed class GetConfigurationInstanceRequest
{
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the instance name.</summary>
    public string Name { get; set; } = string.Empty;
}
