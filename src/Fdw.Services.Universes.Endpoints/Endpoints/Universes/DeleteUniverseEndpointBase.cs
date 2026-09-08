using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Universes.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Soft-deletes a universe.</summary>
public abstract class DeleteUniverseEndpointBase : CrudDeleteEndpointBase<UniverseNameRequest>
{
    private readonly IUniverseConfigurationProvider _provider;
    private readonly IUniverseAccessPolicy _access;

    /// <inheritdoc />
    protected DeleteUniverseEndpointBase(
        ILogger<DeleteUniverseEndpointBase> logger,
        IUniverseConfigurationProvider provider,
        IUniverseAccessPolicy access) : base(logger)
    {
        _provider = provider;
        _access = access;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "universes";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UniverseNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(
        UniverseNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<bool>()
            : GenericResult<bool>.Success(result.Value is not null);
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> Delete(UniverseNameRequest request, CancellationToken ct)
    {
        var found = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (found.IsFailure) return found;

        // The base already established existence, so a null here is an inconsistency, not a 404.
        if (found.Value is null)
        {
            return GenericResult.Failure(
                UniversesResultCodes.ByName("UniverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name));
        }

        // Deleting somebody else's project is the sharpest form of the thing universes:write used
        // to permit across the whole tenant, so it asks the same question the update does.
        var permitted = await _access.MayWrite(found.Value, ct).ConfigureAwait(false);
        return permitted.IsFailure
            ? permitted
            : await _provider.Delete(found.Value.Id, ct).ConfigureAwait(false);
    }
}
