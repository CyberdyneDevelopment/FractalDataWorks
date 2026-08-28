using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Universes.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Soft-deletes a universe.</summary>
public abstract class DeleteUniverseEndpointBase : CrudDeleteEndpointBase<UniverseNameRequest>
{
    private readonly IUniverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected DeleteUniverseEndpointBase(IUniverseConfigurationProvider provider)
    {
        _provider = provider;
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
        return found.Value is null
            ? GenericResult.Failure(
                UniversesResultCodes.ByName("UniverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", request.Name))
            : await _provider.Delete(found.Value.Id, ct).ConfigureAwait(false);
    }
}
