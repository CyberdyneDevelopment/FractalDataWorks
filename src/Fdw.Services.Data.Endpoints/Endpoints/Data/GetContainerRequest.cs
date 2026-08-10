namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for retrieving a container by data store name, path name, and container name.
/// </summary>
public class GetContainerRequest
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path name within the data store.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name within the path.</summary>
    public string ContainerName { get; set; } = string.Empty;
}
