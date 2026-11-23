namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Gives a path back to its data store: <c>DELETE /datastores/{Name}/paths/{PathName}/owner</c> (FDW-6967).
/// </summary>
public abstract class ReleaseDataStorePathOwnerEndpointBase : ReleaseDataStoreItemOwnerEndpointBase<DataStoreItemOwnerRouteRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ReleaseDataStorePathOwnerEndpointBase"/> class.</summary>
    /// <param name="ownership">Releases the owner and decides whether the caller may.</param>
    protected ReleaseDataStorePathOwnerEndpointBase(IDataStoreItemOwnership ownership)
        : base(ownership)
    {
    }

    /// <inheritdoc />
    protected sealed override string Route => "/datastores/{Name}/paths/{PathName}/owner";

    /// <inheritdoc />
    protected sealed override DataStoreItemAddress Address(DataStoreItemOwnerRouteRequest req) => new(req.Name, req.PathName, null);
}
