using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for listing configured data sets, one page at a time.
/// </summary>
/// <remarks>
/// Each data set is read as a record only: its fields, sources, joins and filters are not composed,
/// so the summary carries no counts of them. The fields and sources routes read those on demand (FDW-951).
/// </remarks>
public abstract class ListDataSetsEndpointBase
    : CrudPageEndpointBase<IDataSetImplementationConfiguration, DataSetSummaryResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads the page of data sets.</param>
    protected ListDataSetsEndpointBase(ILogger<ListDataSetsEndpointBase> logger, DataSetConfigurationProvider dataSetProvider)
        : base(logger)
    {
        _dataSetProvider = dataSetProvider ?? throw new ArgumentNullException(nameof(dataSetProvider));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override Task<IGenericResult<ConfigurationPage<IDataSetImplementationConfiguration>>> LoadPage(
        PaginatedListRequest paging, CancellationToken ct)
        => _dataSetProvider.Get(paging.Skip, paging.Take, ConfigurationLevels.RecordOnly, ct);

    /// <inheritdoc />
    protected override Task<IGenericResult<DataSetSummaryResponse>> ToSummary(
        IDataSetImplementationConfiguration item, CancellationToken ct)
        => Task.FromResult(GenericResult<DataSetSummaryResponse>.Success(DataSetQueryHelper.MapToSummary(item)));
}
