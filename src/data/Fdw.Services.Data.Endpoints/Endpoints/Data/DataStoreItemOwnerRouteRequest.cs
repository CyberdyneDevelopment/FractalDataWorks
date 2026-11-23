namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The path a release of its owner gives: <c>DELETE /datastores/{Name}/paths/{PathName}/owner</c> (FDW-6967). A container's route takes
/// <see cref="DataStoreContainerOwnerRouteRequest"/>.
/// </summary>
public class DataStoreItemOwnerRouteRequest
{
    /// <summary>Gets or sets the data store's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the path's name, from the route.</summary>
    public string PathName { get; set; } = string.Empty;
}
