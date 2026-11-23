namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Gives a container back to its data store: <c>DELETE /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/owner</c> (FDW-6967).
/// </summary>
public abstract class ReleaseDataStoreContainerOwnerEndpointBase : ReleaseDataStoreItemOwnerEndpointBase<DataStoreContainerOwnerRouteRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ReleaseDataStoreContainerOwnerEndpointBase"/> class.</summary>
    /// <param name="ownership">Releases the owner and decides whether the caller may.</param>
    protected ReleaseDataStoreContainerOwnerEndpointBase(IDataStoreItemOwnership ownership)
        : base(ownership)
    {
    }

    /// <inheritdoc />
    protected sealed override string Route => "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/owner";

    /// <inheritdoc />
    protected sealed override DataStoreItemAddress Address(DataStoreContainerOwnerRouteRequest req) => new(req.Name, req.PathName, req.ContainerName);
}
