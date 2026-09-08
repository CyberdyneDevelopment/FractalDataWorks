using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Soft-deletes a dataverse.</summary>
public abstract class DeleteDataverseEndpointBase : CrudDeleteEndpointBase<DataverseNameRequest>
{
    private readonly IDataverseConfigurationProvider _provider;
    private readonly IDataverseAccessPolicy _access;

    /// <inheritdoc />
    protected DeleteDataverseEndpointBase(
        ILogger<DeleteDataverseEndpointBase> logger,
        IDataverseConfigurationProvider provider,
        IDataverseAccessPolicy access) : base(logger)
    {
        _provider = provider;
        _access = access;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(DataverseNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(
        DataverseNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<bool>()
            : GenericResult<bool>.Success(result.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(DataverseNameRequest request, CancellationToken ct)
    {
        var found = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found;

        // The base already established existence, so a null here is an inconsistency, not a 404.
        if (found.Value is null)
        {
            return GenericResult.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        // Deleting somebody else's project is the sharpest form of the thing dataverses:write used
        // to permit across the whole tenant, so it asks the same question the update does.
        var permitted = await _access.MayWrite(found.Value, ct).ConfigureAwait(false);
        return permitted.IsFailure
            ? permitted
            : await _provider.Delete(found.Value.Id, ct).ConfigureAwait(false);
    }
}
