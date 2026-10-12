using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Lists data views, one page at a time (FDW-951).</summary>
/// <remarks>
/// Top-level, not scoped to a workspace: a data view is not owned by exactly one workspace -- see
/// <see cref="CreateDataViewEndpointBase"/> for why. A page is one read of the variation domain table, held to the saved
/// views (<see cref="DataViewScope"/>); nothing is composed. What a view draws (encoding, chart type) and its
/// <c>Filters</c> live on its implementation, and the read of one view (<see cref="GetDataViewEndpointBase"/>) carries them
/// (FDW-1032). A view of a view costs one more read of the domain table, for its parent's name.
/// </remarks>
public abstract class ListDataViewsEndpointBase : CrudPageEndpointBase<DataSetVariationDomainConfiguration, DataViewSummaryResponse>
{
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <inheritdoc />
    protected ListDataViewsEndpointBase(ILogger<ListDataViewsEndpointBase> logger, IDataSetVariationConfigurationProvider variations) : base(logger)
    {
        _variations = variations ?? throw new ArgumentNullException(nameof(variations));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "data-views";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<DataSetVariationDomainConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
        => _variations.Get(new DomainQuery(DataViewScope.Filter, null, paging.Skip, paging.Take, false), ct);

    /// <inheritdoc />
    protected override async Task<IGenericResult<DataViewSummaryResponse>> ToSummary(DataSetVariationDomainConfiguration item, CancellationToken ct)
    {
        var parentName = await DataViewStore.ParentNameOf(_variations, item, Logger, ct).ConfigureAwait(false);
        return parentName.IsFailure
            ? parentName.ToNewResult<DataViewSummaryResponse>()
            : GenericResult<DataViewSummaryResponse>.Success(DataViewResponseMapper.ToSummary(item, parentName.Value));
    }
}
