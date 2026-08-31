using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Reads one universe with its members, resources and relationships.</summary>
public abstract class GetUniverseEndpointBase : CrudGetEndpointBase<UniverseNameRequest, UniverseDetailResponse>
{
    private readonly IUniverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetUniverseEndpointBase(IUniverseConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "universes";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UniverseNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<UniverseDetailResponse?>> FindByIdentifier(
        UniverseNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        return result.IsFailure
            ? result.ToNewResult<UniverseDetailResponse?>()
            : GenericResult<UniverseDetailResponse?>.Success(
                result.Value is null ? null : UniverseResponseMapper.ToDetail(result.Value));
    }
}
