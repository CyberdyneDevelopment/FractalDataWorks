using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Abstract base endpoint for previewing data from a DataSet or direct container access.
/// Provides default implementation using <see cref="IDataGateway"/> with virtual hooks
/// for customization.
/// </summary>
public abstract class DataPreviewEndpointBase : Endpoint<DataPreviewRequest, DataPreviewResponse>
{
    private readonly IDataGateway _dataGateway;
    private readonly ILogger<DataPreviewEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataPreviewEndpointBase"/> class.
    /// </summary>
    /// <param name="dataGateway">The data gateway for database operations.</param>
    /// <param name="logger">The logger instance.</param>
    protected DataPreviewEndpointBase(IDataGateway dataGateway, ILogger<DataPreviewEndpointBase>? logger = null)
    {
        _dataGateway = dataGateway;
        _logger = logger ?? NullLogger<DataPreviewEndpointBase>.Instance;
    }

    /// <summary>
    /// Gets the data gateway instance for use by derived classes.
    /// </summary>
    protected IDataGateway DataGateway => _dataGateway;

    /// <summary>
    /// Gets the route for this endpoint. Default is "/data/preview".
    /// </summary>
    protected virtual string Route => "/data/preview";

    /// <summary>
    /// Gets the policy name for authorization. Default is "schema:read".
    /// </summary>
    protected virtual string PolicyName => "schema:read";

    /// <summary>
    /// Gets the maximum allowed value for MaxRows. Default is 1000.
    /// </summary>
    protected virtual int MaxRowsLimit => 1000;

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post(Route);
#if DEVELOP
        AllowAnonymous();
#else
        Policies(PolicyName);
#endif
        Summary(s =>
        {
            s.Summary = "Preview data";
            s.Description = "Returns a preview of data from a DataSet or direct container access.";
        });

        OnBeforeConfiguring();
    }

    /// <summary>
    /// Virtual hook called at the end of <see cref="Configure"/>.
    /// Override to add additional endpoint configuration.
    /// </summary>
    protected virtual void OnBeforeConfiguring()
    {
    }

    /// <summary>
    /// Handles the request by validating input and executing the data preview.
    /// </summary>
    public override async Task HandleAsync(DataPreviewRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.DataSetName) && string.IsNullOrEmpty(req.ContainerName))
        {
            SchemaEndpointLog.PreviewValidationFailed(_logger, "Either DataSetName or ContainerName must be provided");
            ThrowError("Either DataSetName or ContainerName must be provided", 400);
            return;
        }

        var maxRows = Math.Min(Math.Max(req.MaxRows, 1), MaxRowsLimit);
        var source = !string.IsNullOrEmpty(req.DataSetName) ? req.DataSetName : req.ContainerName!;
        SchemaEndpointLog.PreviewingData(_logger, source, maxRows);

        try
        {
            DataPreviewResponse response;

            if (!string.IsNullOrEmpty(req.DataSetName))
            {
                response = await PreviewFromDataSet(req.DataSetName, maxRows, req.Columns, ct).ConfigureAwait(false);
            }
            else
            {
                // Why: fail loud — a missing DataStoreName or PathName is a caller error, not a
                // value to default. The container-address trio (DataStoreName, PathName, ContainerName)
                // must all be present; any missing field is surfaced as a 400 with a structured log.
                if (string.IsNullOrEmpty(req.DataStoreName))
                {
                    SchemaEndpointLog.PreviewContainerAddressMissing(_logger, "DataStoreName");
                    ThrowError("DataStoreName is required when ContainerName is provided", 400);
                    return;
                }

                if (string.IsNullOrEmpty(req.PathName))
                {
                    SchemaEndpointLog.PreviewContainerAddressMissing(_logger, "PathName");
                    ThrowError("PathName is required when ContainerName is provided", 400);
                    return;
                }

                response = await PreviewFromContainer(
                    req.DataStoreName,
                    req.PathName,
                    req.ContainerName!,
                    maxRows,
                    req.Columns,
                    ct).ConfigureAwait(false);
            }

            SchemaEndpointLog.DataPreviewed(_logger, response.Source, response.RowCount);
            await Send.OkAsync(response, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, ex, "preview", source);
            AddError("Data preview failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Previews data from a registered DataSet.
    /// Override to customize DataSet-based preview behavior.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="maxRows">Maximum rows to return.</param>
    /// <param name="columns">Optional columns to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The data preview response.</returns>
    protected virtual async Task<DataPreviewResponse> PreviewFromDataSet(
        string dataSetName,
        int maxRows,
        IList<string>? columns,
        CancellationToken ct)
    {
        ProjectionExpression? projection = BuildProjection(columns);

        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Paging = new PagingExpression { Skip = 0, Take = maxRows },
            Projection = projection
        };

        // Why: DataSet federation routes through DataSetTarget, not DataStoreTarget.
        // The gateway resolves the DataSet name to its underlying sources automatically.
        var result = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, new DataSetTarget(dataSetName), ct).ConfigureAwait(false);
        // Why: a gateway failure must surface as a 500 — returning an empty 200 hides the error from
        // callers and makes a broken query indistinguishable from a query that returned zero rows.
        if (!result.IsSuccess)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, new InvalidOperationException(result.CurrentMessage ?? "Gateway returned failure"), "preview", dataSetName);
            AddError(result.CurrentMessage ?? "DataSet query failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return new DataPreviewResponse { Source = $"DataSet: {dataSetName}", Columns = [], Rows = [], RowCount = 0, HasMoreRows = false };
        }

        var rows = result.Value?.ToList() ?? [];

        var columnDtos = InferColumnsFromRows(rows);

        return new DataPreviewResponse
        {
            Source = $"DataSet: {dataSetName}",
            Columns = columnDtos,
            Rows = rows,
            RowCount = rows.Count,
            HasMoreRows = rows.Count >= maxRows
        };
    }

    /// <summary>
    /// Previews data from a container identified by DataStore+Path+Container address.
    /// Override to customize container-based preview behavior.
    /// </summary>
    /// <param name="dataStoreName">The DataStore name.</param>
    /// <param name="pathName">The path name within the DataStore.</param>
    /// <param name="containerName">The container name within the path.</param>
    /// <param name="maxRows">Maximum rows to return.</param>
    /// <param name="columns">Optional columns to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The data preview response.</returns>
    protected virtual async Task<DataPreviewResponse> PreviewFromContainer(
        string dataStoreName,
        string pathName,
        string containerName,
        int maxRows,
        IList<string>? columns,
        CancellationToken ct)
    {
        ProjectionExpression? projection = BuildProjection(columns);

        // Why: address is DataStoreName.Path/ContainerName — the gateway resolves this
        // to the correct connection, schema, and table. Connection is invisible above this layer.
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Paging = new PagingExpression { Skip = 0, Take = maxRows },
            Projection = projection
        };
        var containerIdentifier = $"{dataStoreName}/{pathName}/{containerName}";

        var result = await _dataGateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, new DataStoreTarget(dataStoreName, pathName, containerName), ct).ConfigureAwait(false);
        // Why: a gateway failure must surface as a 500 — returning an empty 200 hides the error from
        // callers and makes a broken query indistinguishable from a query that returned zero rows.
        if (!result.IsSuccess)
        {
            SchemaEndpointLog.SchemaOperationFailed(_logger, new InvalidOperationException(result.CurrentMessage ?? "Gateway returned failure"), "preview", containerIdentifier);
            AddError(result.CurrentMessage ?? "Container query failed");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return new DataPreviewResponse { Source = $"Container: {containerIdentifier}", Columns = [], Rows = [], RowCount = 0, HasMoreRows = false };
        }

        var rows = result.Value?.ToList() ?? [];

        var columnDtos = InferColumnsFromRows(rows);

        return new DataPreviewResponse
        {
            Source = $"Container: {containerIdentifier}",
            Columns = columnDtos,
            Rows = rows,
            RowCount = rows.Count,
            HasMoreRows = rows.Count >= maxRows
        };
    }

    /// <summary>
    /// Builds a projection expression from the specified column names.
    /// </summary>
    /// <param name="columns">The columns to include, or null for all columns.</param>
    /// <returns>A projection expression, or null if all columns should be included.</returns>
    protected static ProjectionExpression? BuildProjection(IList<string>? columns)
    {
        if (columns == null || columns.Count == 0)
        {
            return null;
        }

        return new ProjectionExpression
        {
            Fields = columns.Select(c => new ProjectionField { PropertyName = c }).ToList()
        };
    }

    /// <summary>
    /// Infers column definitions from the first row of data.
    /// </summary>
    /// <param name="rows">The data rows.</param>
    /// <returns>A list of preview column DTOs.</returns>
    protected static IList<PreviewColumnDto> InferColumnsFromRows(IList<Dictionary<string, object?>> rows)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var firstRow = rows[0];
        return firstRow.Keys.Select(k => new PreviewColumnDto
        {
            Name = k,
            DataType = firstRow[k]?.GetType().Name ?? "Unknown",
            IsKey = false
        }).ToList();
    }
}
