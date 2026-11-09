using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Operations.Logging;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Execution;

/// <summary>
/// Finds the id of the latest run of each of a set of pipeline names on the run rows (<c>etl.PipelineExecution</c>), in a fixed number of requests per piece
/// of names (FDW-4350).
/// </summary>
/// <remarks>
/// Two reads per piece. The first groups the rows of the piece's names by name and takes the newest <c>CreateDate</c> of each; the data gateway cannot return
/// the whole newest row of a group in one statement. The second reads the rows of those names written at those times and keeps, for each name, the row at
/// that name's time. A scoped caller's reads carry its tenant, since a pipeline name is unique per tenant and not across tenants. The run row says which
/// run is the latest; the state of the run is the execution item's, read by the tracker.
/// </remarks>
internal sealed class LatestPipelineRunReader
{
    private const string RunPath = "etl";
    private const string RunContainer = "PipelineExecution";
    private const string NewestColumn = "NewestAt";

    private readonly IDataGateway _gateway;
    private readonly string _dataStoreName;
    private readonly int _chunkSize;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="LatestPipelineRunReader"/> class.</summary>
    /// <param name="gateway">The data gateway the run rows are read through.</param>
    /// <param name="dataStoreName">The operational store the run rows live in.</param>
    /// <param name="chunkSize">How many names are read in one piece; at least 1.</param>
    /// <param name="logger">The logger.</param>
    public LatestPipelineRunReader(IDataGateway gateway, string dataStoreName, int chunkSize, ILogger logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _dataStoreName = dataStoreName ?? throw new ArgumentNullException(nameof(dataStoreName));
        _chunkSize = chunkSize;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Gets the id of the latest run of each name that has one.</summary>
    /// <param name="names">The distinct names to read; none is blank.</param>
    /// <param name="scope">The tenants the caller reaches.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The run id by pipeline name, compared without regard to case; a name with no run is absent.</returns>
    public async Task<IGenericResult<IReadOnlyDictionary<string, Guid>>> LatestIds(
        IReadOnlyList<string> names, ExecutionTenantScope scope, CancellationToken cancellationToken = default)
    {
        var ids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var piece in names.Chunk(_chunkSize))
        {
            var newest = await NewestPerName(piece, scope, cancellationToken).ConfigureAwait(false);
            if (newest.IsFailure)
                return newest.ToNewResult<IReadOnlyDictionary<string, Guid>>();
            if (newest.Value!.Count == 0)
                continue;

            var rows = await RowsAtThoseTimes(newest.Value, scope, cancellationToken).ConfigureAwait(false);
            if (rows.IsFailure)
                return rows.ToNewResult<IReadOnlyDictionary<string, Guid>>();

            foreach (var (name, id) in rows.Value!)
                ids[name] = id;
        }

        return GenericResult<IReadOnlyDictionary<string, Guid>>.Success(ids);
    }

    // The newest CreateDate of each name of the piece, by name.
    private async Task<IGenericResult<IReadOnlyDictionary<string, DateTimeOffset>>> NewestPerName(
        string[] piece, ExecutionTenantScope scope, CancellationToken cancellationToken)
    {
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = Filter(In("PipelineName", piece.Cast<object?>().ToList()), scope),
            Aggregation = new AggregationExpression
            {
                GroupByFields = ["PipelineName"],
                Aggregations = new Dictionary<string, string>(StringComparer.Ordinal) { [NewestColumn] = "MAX(CreateDate)" },
            },
        };
        var read = await _gateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, Target(), cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IReadOnlyDictionary<string, DateTimeOffset>>();

        var newest = new Dictionary<string, DateTimeOffset>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in read.Value ?? [])
        {
            // CreateDate is a datetimeoffset column, and MAX of it is read as the DateTimeOffset it is; any other type is not the column's, and is refused, not read as "no time".
            var name = row.GetValueOrDefault("PipelineName") as string ?? string.Empty;
            var value = row.GetValueOrDefault(NewestColumn);
            if (value is not DateTimeOffset stamp)
            {
                return GenericResult<IReadOnlyDictionary<string, DateTimeOffset>>.Failure(
                    OperationsLog.LatestRunTimeUnreadable(_logger, name, TypeNameOf(value)));
            }

            newest[name] = stamp;
        }

        return GenericResult<IReadOnlyDictionary<string, DateTimeOffset>>.Success(newest);
    }

    // The rows of those names written at those times; each name keeps the row written at its own newest time.
    private async Task<IGenericResult<IReadOnlyDictionary<string, Guid>>> RowsAtThoseTimes(
        IReadOnlyDictionary<string, DateTimeOffset> newest, ExecutionTenantScope scope, CancellationToken cancellationToken)
    {
        // CreateDate is a Metadata field, which a read leaves out unless it names it (FDW-988); this read matches each row on it, so it names the
        // columns it reads (OPERATIONS-61153).
        var command = new QueryCommand<Dictionary<string, object?>>
        {
            Filter = Filter(
                In("PipelineName", newest.Keys.Cast<object?>().ToList()),
                In("CreateDate", newest.Values.Distinct().Cast<object?>().ToList()),
                scope),
            Projection = new ProjectionExpression
            {
                Fields = [new ProjectionField { PropertyName = "Id" }, new ProjectionField { PropertyName = "PipelineName" }, new ProjectionField { PropertyName = "CreateDate" }],
            },
        };
        var read = await _gateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, Target(), cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
            return read.ToNewResult<IReadOnlyDictionary<string, Guid>>();

        var ids = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in read.Value ?? [])
        {
            // Id is a uniqueidentifier and CreateDate a datetimeoffset: each is read as the type it is, and a value of any other type is refused naming it.
            var name = row.GetValueOrDefault("PipelineName") as string ?? string.Empty;
            var created = row.GetValueOrDefault("CreateDate");
            if (created is not DateTimeOffset createdAt)
            {
                return GenericResult<IReadOnlyDictionary<string, Guid>>.Failure(
                    OperationsLog.LatestRunTimeUnreadable(_logger, name, TypeNameOf(created)));
            }

            var id = row.GetValueOrDefault("Id");
            if (id is not Guid runId)
            {
                return GenericResult<IReadOnlyDictionary<string, Guid>>.Failure(
                    OperationsLog.LatestRunIdUnreadable(_logger, name, TypeNameOf(id)));
            }

            if (!newest.TryGetValue(name, out var at) || createdAt != at)
                continue;

            // Two runs written at the same instant: the greater id is kept, so the answer does not depend on the order the rows came back in.
            if (!ids.TryGetValue(name, out var kept) || runId.CompareTo(kept) > 0)
                ids[name] = runId;
        }

        return GenericResult<IReadOnlyDictionary<string, Guid>>.Success(ids);
    }

    // The name of a value's type for a refusal; a value that is absent is a null from the store.
    private static string TypeNameOf(object? value) => value is null ? nameof(DBNull) : value.GetType().Name;

    private DataStoreTarget Target() => new(_dataStoreName, RunPath, RunContainer);

    private static FilterCondition In(string property, List<object?> values)
        => new() { PropertyName = property, Operator = FilterOperators.ByName("In"), Value = values };

    private static FilterExpression Filter(FilterCondition names, ExecutionTenantScope scope)
        => Filter([names], scope);

    private static FilterExpression Filter(FilterCondition names, FilterCondition times, ExecutionTenantScope scope)
        => Filter([names, times], scope);

    // The conditions, and the caller's tenant when the caller does not reach every tenant.
    private static FilterExpression Filter(List<FilterCondition> conditions, ExecutionTenantScope scope)
    {
        if (!scope.ReachesAll)
        {
            conditions.Add(new FilterCondition
            {
                PropertyName = "TenantId",
                Operator = FilterOperators.ByName("Equal"),
                Value = scope.TenantId,
            });
        }

        return conditions.Count == 1
            ? new FilterExpression { Root = conditions[0] }
            : new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = conditions.Cast<IFilterNode>().ToList() } };
    }
}
