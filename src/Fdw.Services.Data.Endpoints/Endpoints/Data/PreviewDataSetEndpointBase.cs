using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Generic base endpoint for previewing data from a DataSet.
/// Loads the DataSet configuration via DataSetConfigurationProvider (including Fields and Sources),
/// then queries the first active source for sample rows.
/// </summary>
public abstract class PreviewDataSetEndpointBase : CrudGetEndpoint<PreviewDataSetRequest, PreviewDataSetResponse>
{
    // Why: DataSetConfigurationProvider.Get(name) composes Fields, Sources, and FieldMappings —
    // no separate source resolver or secondary IDataGateway config reads needed.
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>Gets the data gateway for executing live data preview queries.</summary>
    protected IDataGateway DataGateway { get; }

    /// <summary>Gets the logger.</summary>
    protected new ILogger<PreviewDataSetEndpointBase> Logger { get; }

    /// <inheritdoc />
    protected PreviewDataSetEndpointBase(
        DataSetConfigurationProvider dataSetProvider,
        IDataGateway dataGateway,
        ILogger<PreviewDataSetEndpointBase> logger)
    {
        _dataSetProvider = dataSetProvider;
        DataGateway = dataGateway;
        Logger = logger ?? NullLogger<PreviewDataSetEndpointBase>.Instance;
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
    /// Loads the DataSet configuration (including Fields and SourceIds), resolves the first active source,
    /// then queries sample rows from that source.
    /// Override for custom preview behavior.
    /// </summary>
    protected virtual async Task<IGenericResult<PreviewDataSetResponse?>> PreviewDataSet(
        string dataSetName,
        int maxRows,
        CancellationToken ct)
    {
        DataSetEndpointLog.LoadingDataSet(Logger, dataSetName, string.Empty);

        // Why: Get(name) calls AssembleHierarchy which populates Fields and SourceIds in parallel.
        var dsResult = await _dataSetProvider.Get(dataSetName, ct).ConfigureAwait(false);
        if (dsResult.IsFailure) return dsResult.ToNewResult<PreviewDataSetResponse?>();

        if (dsResult.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, dataSetName);
            return GenericResult<PreviewDataSetResponse?>.Success((PreviewDataSetResponse?)null);
        }

        var config = dsResult.Value;

        // Build columns from the Fields already assembled by AssembleHierarchy.
        var columns = config.Fields
            .OrderBy(f => f.Ordinal)
            .Select(f => new PreviewColumnDto
            {
                Name = f.Name,
                DataType = f.TypeName,
                IsNullable = !f.IsRequired
            })
            .ToList();

        // Why: Sources are part of the composed aggregate returned by DataSetConfigurationProvider.Get.
        DataSetEndpointLog.LoadingSources(Logger, dataSetName);
        var (rows, hasMore) = await FetchPreviewRows(
            dataSetName,
            config.Sources as IReadOnlyList<DataSetSourceConfiguration> ?? config.Sources?.ToList(),
            maxRows,
            ct).ConfigureAwait(false);

        return GenericResult<PreviewDataSetResponse?>.Success(new PreviewDataSetResponse
        {
            DataSetName = dataSetName,
            Columns = columns,
            Rows = rows,
            HasMoreRows = hasMore
        });
    }

    private async Task<(List<IReadOnlyDictionary<string, object?>> Rows, bool HasMore)> FetchPreviewRows(
        string dataSetName,
        IReadOnlyList<DataSetSourceConfiguration>? sources,
        int maxRows,
        CancellationToken ct)
    {
        var source = sources?
            .Where(s => s.IsCurrent && !s.IsDeleted)
            .OrderBy(s => s.Priority)
            .FirstOrDefault();

        if (source is null || string.IsNullOrEmpty(source.DataStoreName) || string.IsNullOrEmpty(source.ContainerName))
            return ([], false);

        // Why: Addressing moved off IDataCommand onto DataStoreTarget. Path and ContainerName
        // are passed separately so the MsSql translator can qualify the table name as schema.table.
        var previewCommand = new QueryCommand<ExpandoObject>
        {
            Paging = new PagingExpression { Skip = 0, Take = maxRows + 1 }
        };
        var previewResult = await DataGateway
            .Execute<System.Collections.Generic.IEnumerable<ExpandoObject>>(
                previewCommand,
                new DataStoreTarget(source.DataStoreName, source.Path, source.ContainerName),
                ct)
            .ConfigureAwait(false);

        if (previewResult.IsFailure || previewResult.Value is null)
        {
            DataSetEndpointLog.PreviewRowsFetchFailed(Logger, dataSetName);
            return ([], false);
        }

        var allRows = previewResult.Value.ToList();
        var hasMore = allRows.Count > maxRows;

        var rows = allRows
            .Take(maxRows)
            .Select(r => (IReadOnlyDictionary<string, object?>)
                ((System.Collections.Generic.IDictionary<string, object?>)(object)r)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    System.StringComparer.Ordinal))
            .ToList();

        return (rows, hasMore);
    }
}
