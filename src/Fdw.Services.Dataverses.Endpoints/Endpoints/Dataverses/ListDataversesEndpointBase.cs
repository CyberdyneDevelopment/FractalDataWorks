using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Lists the dataverses visible to the caller.</summary>
public abstract class ListDataversesEndpointBase : CrudListEndpointBase<DataverseSummaryResponse>
{
    private readonly IDataverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected ListDataversesEndpointBase(ILogger<ListDataversesEndpointBase> logger, IDataverseConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "dataverses";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataverseSummaryResponse>>> LoadItems(CancellationToken ct)
    {
        var result = await _provider.Get(ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<List<DataverseSummaryResponse>>();

        // Why not `?? []`: an empty list is a real answer meaning "no dataverses". Substituting one
        // for a null would report that answer for an internal inconsistency instead.
        return result.Value is null
            ? GenericResult<List<DataverseSummaryResponse>>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", "(list)"))
            : GenericResult<List<DataverseSummaryResponse>>.Success(
                result.Value.Select(DataverseResponseMapper.ToSummary).ToList());
    }
}
