using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Dataverses.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Lists every saved view.</summary>
/// <remarks>
/// Top-level, not scoped to a dataverse: a saved view is not owned by exactly one dataverse -- see
/// <see cref="CreateSavedViewEndpointBase"/> for why.
/// </remarks>
public abstract class ListSavedViewsEndpointBase : CrudListEndpointBase<SavedViewResponse>
{
    private readonly ISavedViewConfigurationProvider _provider;

    /// <inheritdoc />
    protected ListSavedViewsEndpointBase(ILogger<ListSavedViewsEndpointBase> logger, ISavedViewConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "saved-views";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<SavedViewResponse>>> LoadItems(CancellationToken ct)
    {
        var result = await _provider.Get(ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<List<SavedViewResponse>>();

        return result.Value is null
            ? GenericResult<List<SavedViewResponse>>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), Logger,
                ResultDetails.Create("name", "(list)"))
            : GenericResult<List<SavedViewResponse>>.Success(
                result.Value.Select(SavedViewResponseMapper.ToResponse).ToList());
    }
}
