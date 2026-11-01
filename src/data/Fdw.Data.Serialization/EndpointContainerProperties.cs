namespace Fdw.Data.Serialization;

/// <summary>
/// Properties specific to HTTP endpoint containers.
/// </summary>
public sealed class EndpointContainerProperties : IContainerProperties
{
    /// <summary>
    /// Gets or sets the HTTP methods supported by this endpoint.
    /// </summary>
    public required string[] HttpMethods { get; init; }
}