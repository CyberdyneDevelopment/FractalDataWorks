using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Pagination;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for previewing data from a DataSet.
/// Loads the DataSet configuration via DataSetConfigurationProvider for its columns, then reads sample
/// rows from the data set itself.
/// </summary>
/// <remarks>
/// The rows are read through <see cref="DataSetQuery"/>, the one path every route that returns a data set's rows
/// takes (FDW-1075): the data set's read gate, the strategy's renames, transformations and calculated fields, and
/// each row cut down to the data set's declared fields. A preview shows exactly what a consumer of the data set
/// receives, never a column of the table under it that the data set does not declare.
/// </remarks>
public abstract class PreviewDataSetEndpointBase : CrudGetEndpointBase<PreviewDataSetRequest, PreviewDataSetResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly DataSetQuery _query;

    /// <summary>Gets the logger.</summary>
    protected new ILogger<PreviewDataSetEndpointBase> Logger { get; }

    /// <inheritdoc />
    protected PreviewDataSetEndpointBase(
        DataSetConfigurationProvider dataSetProvider,
        IDataGatewayProvider dataGateways,
        ILogger<PreviewDataSetEndpointBase> logger)
        : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        Logger = logger ?? NullLogger<PreviewDataSetEndpointBase>.Instance;
        _query = new DataSetQuery(dataGateways, Logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(PreviewDataSetRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<PreviewDataSetResponse?>> FindByIdentifier(
        PreviewDataSetRequest request,
        CancellationToken ct)
        => PreviewDataSet(request.Name, request.MaxRows, ct);

    /// <summary>
    /// Loads the DataSet configuration for its columns, then reads sample rows from the data set.
    /// Override for custom preview behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<PreviewDataSetResponse?>> PreviewDataSet(
        string dataSetName,
        int maxRows,
        CancellationToken ct)
    {
        DataSetEndpointLog.LoadingDataSet(Logger, dataSetName, string.Empty);

        var dsResult = await _dataSetProvider.Get(dataSetName, ct).ConfigureAwait(false);
        if (dsResult.IsFailure) return dsResult.ToNewResult<PreviewDataSetResponse?>();

        if (dsResult.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, dataSetName);
            return GenericResult<PreviewDataSetResponse?>.Success((PreviewDataSetResponse?)null);
        }

        var config = dsResult.Value;

        var columns = config.Fields
            .Where(f => f.IsCurrent && !f.IsDeleted)
            .OrderBy(f => f.Ordinal)
            .Select(f => new PreviewColumnDto
            {
                Name = f.Name,
                DataType = f.TypeName,
                IsNullable = !f.IsRequired
            })
            .ToList();

        DataSetEndpointLog.LoadingSources(Logger, dataSetName);
        var rowCap = PaginationQuery.ResolveCalculatedRowCap(HttpContext, Logger);
        if (rowCap.IsFailure) return rowCap.ToNewResult<PreviewDataSetResponse?>();

        var page = await _query.Run(
            config, new DataQueryRequest { DataSetName = dataSetName, Take = maxRows }, maxRows, rowCap.Value, DataSetQuery.Unscoped, ct)
            .ConfigureAwait(false);
        if (page.IsFailure)
        {
            DataSetEndpointLog.PreviewRowsFetchFailed(Logger, dataSetName);
            return page.ToNewResult<PreviewDataSetResponse?>();
        }

        var read = page.Value!;
        return GenericResult<PreviewDataSetResponse?>.Success(new PreviewDataSetResponse
        {
            DataSetName = dataSetName,
            Columns = columns,
            Rows = read.Rows,
            HasMoreRows = read.HasMoreRows
        });
    }
}
