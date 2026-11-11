using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Counts the data sets the caller may see by status, for a console that shows how many need attention or are running (FDW-4488).
/// </summary>
/// <remarks>
/// Every status of <c>DataSetStatuses</c> is listed, highest rank first, with the number of data sets that have it, so a status nothing has reads 0 and a status a
/// developer added appears. The data set domain counts them (<see cref="IDataSetStatuses.Counts"/>): the endpoint only maps the answer.
/// </remarks>
public abstract class ListDataSetStatusCountsEndpointBase : CrudListEndpointBase<DataSetStatusCountDto>
{
    private readonly IDataSetStatuses _statuses;

    /// <summary>Initializes a new instance of the <see cref="ListDataSetStatusCountsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="statuses">The data set domain's answer to how many data sets have each status.</param>
    protected ListDataSetStatusCountsEndpointBase(ILogger<ListDataSetStatusCountsEndpointBase> logger, IDataSetStatuses statuses)
        : base(logger)
    {
        _statuses = statuses;
    }

    /// <summary>Gets the resource name used for route generation.</summary>
    protected override string ResourceName => "dataset-status-counts";

    /// <summary>Gated by "datasets:read": the counts are over the data sets the caller may read.</summary>
    protected override string ReadPolicy => "datasets:read";

    /// <inheritdoc />
    protected override async Task<IGenericResult<List<DataSetStatusCountDto>>> LoadItems(CancellationToken ct)
    {
        var counts = await _statuses.Counts(ct).ConfigureAwait(false);
        return counts.IsFailure || counts.Value is null
            ? counts.ToNewResult<List<DataSetStatusCountDto>>()
            : GenericResult<List<DataSetStatusCountDto>>.Success([.. counts.Value.Select(count => new DataSetStatusCountDto { Status = count.Status.Name, Count = count.Count })]);
    }
}
