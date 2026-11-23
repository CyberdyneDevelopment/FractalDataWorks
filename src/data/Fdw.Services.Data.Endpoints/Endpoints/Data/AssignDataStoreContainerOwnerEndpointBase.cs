namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Makes a workspace the owner of a container of a shared data store: <c>POST /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/owner</c> (FDW-6967).
/// </summary>
public abstract class AssignDataStoreContainerOwnerEndpointBase : AssignDataStoreItemOwnerEndpointBase<AssignDataStoreContainerOwnerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="AssignDataStoreContainerOwnerEndpointBase"/> class.</summary>
    /// <param name="ownership">Records the owner and decides whether the caller may.</param>
    protected AssignDataStoreContainerOwnerEndpointBase(IDataStoreItemOwnership ownership)
        : base(ownership)
    {
    }

    /// <inheritdoc />
    protected sealed override string Route => "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/owner";

    /// <inheritdoc />
    protected sealed override DataStoreItemAddress Address(AssignDataStoreContainerOwnerRequest req) => new(req.Name, req.PathName, req.ContainerName);
}
