
namespace Fdw.Services.Data.Endpoints;

/// <summary>A request addressed to the data option rules of one container (FDW-6339).</summary>
public class DataOptionContainerRequest
{
    /// <summary>Gets or sets the data store name (route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path (route).</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container name (route).</summary>
    public string ContainerName { get; set; } = string.Empty;
}
