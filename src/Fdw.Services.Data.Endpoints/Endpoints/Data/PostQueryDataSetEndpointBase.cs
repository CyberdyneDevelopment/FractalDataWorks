using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for POST-body DataSet queries.
/// Route: POST /datasets/{DataSetName}/query
/// Accepts filters in the request body rather than query string, enabling
/// complex multi-field filters without URL length limits.
/// </summary>
public abstract class PostQueryDataSetEndpointBase : Endpoint<PostQueryDataSetRequest, DataSetQueryResponse>
{
    // Why: DataSetConfigurationProvider (config records) is injected here — not IDataSetProvider
    // (runtime service). This endpoint needs DataSetConfiguration to inspect Fields and Sources
    // for query construction. The provider's Get(name) composes Fields, Sources, and FieldMappings.
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly IDataGateway _dataGateway;

    /// <summary>Gets the logger.</summary>
    // Why: FastEndpoints Endpoint<TReq,TRes> also exposes Logger; 'new' suppresses the hide warning
    // while intentionally shadowing with a concrete ILogger for typed message-logging.
    protected new ILogger<PostQueryDataSetEndpointBase> Logger { get; }

    /// <inheritdoc cref="PostQueryDataSetEndpointBase"/>
    protected PostQueryDataSetEndpointBase(
        DataSetConfigurationProvider dataSetProvider,
        IDataGateway dataGateway,
        ILogger<PostQueryDataSetEndpointBase> logger)
    {
        _dataSetProvider = dataSetProvider;
        _dataGateway = dataGateway;
        // Why: NullLogger fallback ensures the endpoint is functional even if DI
        // doesn't wire up logging (e.g. in tests without a full DI container).
        Logger = logger ?? NullLogger<PostQueryDataSetEndpointBase>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/datasets/{DataSetName}/query");
        Tags("DataSets");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Query a DataSet (POST)";
            s.Description =
                "Queries a registered DataSet by name. Filters are supplied as a field-name → value " +
                "dictionary in the request body. All conditions are ANDed together. " +
                "Prefer this over GET /datasets/{DataSetName}/query when passing multiple filters " +
                "or when filter values would exceed URL length limits.";
        });
    }

    /// <inheritdoc />
    // Why: Sequential DataSet query pipeline — resolve config, validate filters, resolve source,
    // build command, execute, shape response. Each branch is a distinct error path; extracting
    // sub-methods would scatter the single-pass flow without reducing actual complexity.
#pragma warning disable FDW006
    public override async Task HandleAsync(PostQueryDataSetRequest req, CancellationToken ct)
#pragma warning restore FDW006
    {
        var take = Math.Clamp(req.Take, 1, 1000);
        var skip = Math.Max(req.Skip, 0);
        var filters = req.Filters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        DataSetEndpointLog.PostQueryingDataSet(Logger, req.DataSetName, filters.Count, skip, take);

        // Resolve DataSet configuration by name — provider composes Fields, Sources, and FieldMappings.
        var dataSetResult = await _dataSetProvider.Get(req.DataSetName, ct).ConfigureAwait(false);
        if (dataSetResult.IsFailure || dataSetResult.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, req.DataSetName);
            await Send.NotFoundAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        var fields = dataSetResult.Value.Fields;

        // Build column metadata
        var columns = fields
            .OrderBy(f => f.Ordinal)
            .Select(f => new DataSetQueryColumnDto
            {
                Name = f.Name,
                DataType = f.TypeName,
                IsKey = f.IsKey,
                IsIndexed = f.IsIndexed,
                Role = f.Role
            })
            .ToList();

        // Validate and apply body filters
        var fieldNames = new HashSet<string>(
            fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase);
        var appliedFilters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (fieldName, value) in filters)
        {
            if (!fieldNames.Contains(fieldName))
            {
                DataSetEndpointLog.PostQueryUnknownFilterField(Logger, req.DataSetName, fieldName);
                continue;
            }
            if (!string.IsNullOrEmpty(value))
                appliedFilters[fieldName] = value;
        }

        // Why: Sources are part of the composed aggregate returned by DataSetConfigurationProvider.Get.
        var primarySource = dataSetResult.Value.Sources?
            .Where(s => s.IsCurrent && !s.IsDeleted)
            .OrderBy(s => s.Priority)
            .FirstOrDefault();

        if (primarySource is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, req.DataSetName);
            await Send.NotFoundAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrEmpty(primarySource.ContainerName))
        {
            DataSetEndpointLog.DataSetNotFound(Logger, req.DataSetName);
            await Send.NotFoundAsync(cancellation: ct).ConfigureAwait(false);
            return;
        }

        // Build and execute query.
        // Why: Addressing moved off IDataCommand onto DataStoreTarget. DataStoreName on the source
        // is the data store connection — not the config connection. Path (schema) and ContainerName
        // are passed separately so the translator can qualify the table name correctly.
        // No fallback: if DataStoreName is missing the query will fail loud rather than silently
        // routing to the wrong connection.
        // Why: Dictionary<string, object?> is the framework's generic row -- a dataset's columns are
        // known only at runtime, and this is the shape the connections materialize and
        // DataSetExecutionHelpers converts without a mapper. ExpandoObject is not a supported row
        // shape: it reaches ConvertToPocos, finds no generated mapper, and the rows are lost.
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Paging = new PagingExpression { Skip = skip, Take = take + 1 },
            Filter = BuildFilter(appliedFilters)
        };

        var queryResult = await _dataGateway
            .Execute<IEnumerable<Dictionary<string, object?>>>(
                command,
                new DataStoreTarget(primarySource.DataStoreName, primarySource.PathValue, primarySource.ContainerName),
                ct)
            .ConfigureAwait(false);

        if (queryResult.IsFailure)
        {
            DataSetEndpointLog.PostQueryFailed(Logger, req.DataSetName,
                queryResult.CurrentMessage ?? "Unknown error");
            await Send.ResponseAsync(new DataSetQueryResponse(), 500, ct).ConfigureAwait(false);
            return;
        }

        var allRows = (queryResult.Value ?? Enumerable.Empty<Dictionary<string, object?>>()).ToList();
        var hasMore = allRows.Count > take;

        var rows = allRows
            .Take(take)
            .Select(r => (IReadOnlyDictionary<string, object?>)
                new Dictionary<string, object?>(r, StringComparer.Ordinal))
            .ToList();

        DataSetEndpointLog.PostQueryCompleted(Logger, req.DataSetName, rows.Count, hasMore);

        await Send.OkAsync(new DataSetQueryResponse
        {
            DataSetName = req.DataSetName,
            Columns = columns,
            Rows = rows,
            Skip = skip,
            Take = take,
            HasMoreRows = hasMore,
            AppliedFilters = appliedFilters
        }, cancellation: ct).ConfigureAwait(false);
    }

    private static FilterExpression? BuildFilter(Dictionary<string, string> filters)
    {
        if (filters.Count == 0)
            return null;

        var conditions = new List<IFilterNode>();
        foreach (var (fieldName, value) in filters)
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = fieldName,
                Operator = FilterOperators.ByName("Equal"),
                Value = value
            });
        }

        if (conditions.Count == 1)
            return new FilterExpression { Root = conditions[0] };

        return new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes = conditions
            }
        };
    }
}
