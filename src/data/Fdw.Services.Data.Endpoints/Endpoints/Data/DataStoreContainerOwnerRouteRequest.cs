namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The container a release of its owner gives: <c>DELETE /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/owner</c> (FDW-6967).
/// </summary>
public class DataStoreContainerOwnerRouteRequest : DataStoreItemOwnerRouteRequest
{
    /// <summary>Gets or sets the container's name, from the route.</summary>
    public string ContainerName { get; set; } = string.Empty;
}
