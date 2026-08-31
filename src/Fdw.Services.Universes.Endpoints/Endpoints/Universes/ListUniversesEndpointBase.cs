using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Universes.Results;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Lists the universes visible to the caller.</summary>
public abstract class ListUniversesEndpointBase : CrudListEndpointBase<UniverseSummaryResponse>
{
    private readonly IUniverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected ListUniversesEndpointBase(IUniverseConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "universes";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<UniverseSummaryResponse>>> LoadItems(CancellationToken ct)
    {
        var result = await _provider.Get(ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<List<UniverseSummaryResponse>>();

        // Why not `?? []`: an empty list is a real answer meaning "no universes". Substituting one
        // for a null would report that answer for an internal inconsistency instead.
        return result.Value is null
            ? GenericResult<List<UniverseSummaryResponse>>.Failure(
                UniversesResultCodes.ByName("UniverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", "(list)"))
            : GenericResult<List<UniverseSummaryResponse>>.Success(
                result.Value.Select(UniverseResponseMapper.ToSummary).ToList());
    }
}
