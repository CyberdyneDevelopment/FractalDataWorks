namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Makes a workspace the owner of a path of a shared data store: <c>POST /datastores/{Name}/paths/{PathName}/owner</c> (FDW-6967).
/// </summary>
public abstract class AssignDataStorePathOwnerEndpointBase : AssignDataStoreItemOwnerEndpointBase<AssignDataStoreItemOwnerRequest>
{
    /// <summary>Initializes a new instance of the <see cref="AssignDataStorePathOwnerEndpointBase"/> class.</summary>
    /// <param name="ownership">Records the owner and decides whether the caller may.</param>
    protected AssignDataStorePathOwnerEndpointBase(IDataStoreItemOwnership ownership)
        : base(ownership)
    {
    }

    /// <inheritdoc />
    protected sealed override string Route => "/datastores/{Name}/paths/{PathName}/owner";

    /// <inheritdoc />
    protected sealed override DataStoreItemAddress Address(AssignDataStoreItemOwnerRequest req) => new(req.Name, req.PathName, null);
}
