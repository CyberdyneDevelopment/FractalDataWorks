using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Services.Data.Logging;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Reads one page of a data set the way every route that returns its rows reads it (FDW-1075).
/// </summary>
/// <remarks>
/// <para>
/// One path, so the routes cannot drift apart: the data set's own read gate (<see cref="DataSetReadGate"/>), the
/// caller's filter refused on a field the data set has not made filterable, the read dispatched through the gateway's
/// <see cref="DataSetTarget"/> (so the strategy renames to logical fields, runs each mapping's transformations and adds
/// calculated fields), and each row cut down to the data set's declared fields.
/// </para>
/// <para>
/// The grant is the data set, not the table under it. A route that read the first source's container directly
/// answered with every column of that table, raw, whatever the data set declared; no route does that now.
/// </para>
/// </remarks>
public sealed class DataSetQuery
{
    private readonly IDataGatewayProvider? _dataGateways;
    private readonly IFilterCalculations? _calculations;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DataSetQuery"/> class.</summary>
    /// <param name="dataGateways">Supplies the gateway the read is dispatched through.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="calculations">
    /// Evaluates the calculations a filter names for its values; null for a host that has none, whose queries are refused when a
    /// filter names one.
    /// </param>
    public DataSetQuery(IDataGatewayProvider dataGateways, ILogger? logger = null, IFilterCalculations? calculations = null)
    {
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
        _calculations = calculations;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Initializes a new instance of the <see cref="DataSetQuery"/> class for a caller that hands over the gateway on every read.</summary>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <param name="calculations">
    /// Evaluates the calculations a filter names for its values; null for a host that has none, whose queries are refused when a
    /// filter names one.
    /// </param>
    /// <remarks>
    /// Only <see cref="Run(IDataGateway, IDataSetImplementationConfiguration, DataQueryRequest, int, int, Func{CancellationToken, Task{IGenericResult{QueryFilterNode?}}}, CancellationToken)"/>
    /// can be called on a query made this way; the overload that finds the gateway through a provider throws.
    /// </remarks>
    public DataSetQuery(ILogger? logger = null, IFilterCalculations? calculations = null)
    {
        _calculations = calculations;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>The route-added filter of a route that adds none.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A success carrying no filter.</returns>
    public static Task<IGenericResult<QueryFilterNode?>> Unscoped(CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<QueryFilterNode?>.Success(null));

    // Why resolved per call rather than held: the gateway is scoped and a caller may not be.
    private IDataGateway Gateway => _dataGateways is { } gateways
        ? gateways.ByName("Main")
        : throw new InvalidOperationException("This query was made without a gateway provider; pass the gateway to Run.");

    /// <summary>Reads one page of <paramref name="dataSet"/> through the provider's "Main" gateway.</summary>
    /// <param name="dataSet">The data set, already resolved as the caller.</param>
    /// <param name="request">The filter, order, page and grouping asked for. Its filter is replaced by the one actually run, the route's scope ANDed in.</param>
    /// <param name="take">The page size, already resolved from the paging settings.</param>
    /// <param name="rowCap">The most rows a read that filters or sorts on a calculated field may read (FDW-1045).</param>
    /// <param name="scope">The filter the route puts on every read of this data set, checked against its fields with the caller's.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page, or the refusal the caller is answered with.</returns>
    public Task<IGenericResult<DataQueryPage>> Run(
        IDataSetImplementationConfiguration dataSet,
        DataQueryRequest request,
        int take,
        int rowCap,
        Func<CancellationToken, Task<IGenericResult<QueryFilterNode?>>> scope,
        CancellationToken cancellationToken = default)
        => RunWith(() => Gateway, dataSet, request, take, rowCap, scope, cancellationToken);

    /// <summary>Reads one page of <paramref name="dataSet"/> through <paramref name="gateway"/>, the gateway the caller holds.</summary>
    /// <param name="gateway">The gateway the read is dispatched through, so the data store applies the caller's row-level security.</param>
    /// <param name="dataSet">The data set, already resolved as the caller.</param>
    /// <param name="request">The filter, order, page and grouping asked for. Its filter is replaced by the one actually run, the route's scope ANDed in.</param>
    /// <param name="take">The page size, already resolved from the paging settings.</param>
    /// <param name="rowCap">The most rows a read that filters or sorts on a calculated field may read (FDW-1045).</param>
    /// <param name="scope">The filter the route puts on every read of this data set, checked against its fields with the caller's.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The page, or the refusal the caller is answered with.</returns>
    public Task<IGenericResult<DataQueryPage>> Run(
        IDataGateway gateway,
        IDataSetImplementationConfiguration dataSet,
        DataQueryRequest request,
        int take,
        int rowCap,
        Func<CancellationToken, Task<IGenericResult<QueryFilterNode?>>> scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gateway);
        return RunWith(() => gateway, dataSet, request, take, rowCap, scope, cancellationToken);
    }

    // Why the gateway is a function: the provider's gateway is resolved per call and only once the query has translated, so a refused
    // query never asks the provider for one.
    private async Task<IGenericResult<DataQueryPage>> RunWith(
        Func<IDataGateway> gateway,
        IDataSetImplementationConfiguration dataSet,
        DataQueryRequest request,
        int take,
        int rowCap,
        Func<CancellationToken, Task<IGenericResult<QueryFilterNode?>>> scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dataSet);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(scope);

        var queryable = DataQueryGrouping.IsRequested(request) ? DataSetReadGate.Groupable(dataSet, _logger) : DataSetReadGate.Pageable(dataSet, _logger);
        if (queryable.IsFailure) return queryable.ToNewResult<DataQueryPage>();

        var translator = DataQueryTranslator.Create(dataSet.Fields.Where(f => f.IsCurrent && !f.IsDeleted), dataSet.Name, _logger);
        if (translator.IsFailure) return translator.ToNewResult<DataQueryPage>();

        // Why before the scope merge: filterability limits what a caller may ask for, and a view's own stored filter may
        // name a field the caller may not.
        var filterable = translator.Value!.RequireFilterable(request.Filter);
        if (filterable.IsFailure) return filterable.ToNewResult<DataQueryPage>();

        var scoped = await scope(cancellationToken).ConfigureAwait(false);
        if (scoped.IsFailure) return scoped.ToNewResult<DataQueryPage>();

        // Why after the merge: a view's stored filter may name a calculation too, and the whole filter is resolved once, so the
        // query that runs holds only literals. The tree the view stored is not changed; it keeps the calculation's name and inputs.
        var resolved = await translator.Value!.ResolveCalculations(
            DataQueryScope.Combine(scoped.Value, request.Filter), _calculations, cancellationToken).ConfigureAwait(false);
        if (resolved.IsFailure) return resolved.ToNewResult<DataQueryPage>();

        request.Filter = resolved.Value;

        var query = Command(translator.Value!, request, take, rowCap);
        if (query.IsFailure) return query.ToNewResult<DataQueryPage>();

        return await Read(gateway, request, dataSet.Name, query.Value!, take, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs the store's part of the query, then applies the part that names a calculated field to the rows it read, and cuts the page.</summary>
    private async Task<IGenericResult<DataQueryPage>> Read(
        Func<IDataGateway> gateway, DataQueryRequest request, string dataSetName, GroupedQuery query, int take, CancellationToken cancellationToken)
    {
        var rows = await gateway().Execute<IEnumerable<Dictionary<string, object?>>>(
            query.Command, new DataSetTarget(dataSetName), cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure) return rows.ToNewResult<DataQueryPage>();
        if (rows.Value is null)
            return GenericResult<DataQueryPage>.Failure(
                DataServiceResultCodes.ByName("DataQueryReturnedNoValue"), _logger,
                ResultDetails.Create("dataSet", dataSetName));

        var read = rows.Value.ToList();
        return query.Plan is { RunsInMemory: true } plan
            ? InMemory(request, dataSetName, read, query, plan, take)
            : GenericResult<DataQueryPage>.Success(Page(request, dataSetName, read, query.Columns, take));
    }

    /// <summary>Applies a calculated field's filter and sort to the rows the store handed over, refusing a read over the cap rather than cutting it short (FDW-1045).</summary>
    /// <remarks>
    /// A calculated field has no value in the store, so the store was asked for every row its own part of the filter keeps, up to
    /// one past the cap. One past the cap means the filter on the other fields was too wide for this query to be answered whole,
    /// and an answer built from part of the rows would be wrong, so it is refused and says why.
    /// </remarks>
    private IGenericResult<DataQueryPage> InMemory(
        DataQueryRequest request, string dataSetName, List<Dictionary<string, object?>> read, GroupedQuery query, QueryPlan plan, int take)
    {
        if (read.Count > plan.RowCap)
            return new DataQueryRefusals(dataSetName, _logger).Invalid<DataQueryPage>(
                $"filtering or sorting on a calculated field reads every matching row, and this query matches more than {plan.RowCap}; narrow the filter on the data set's other fields");

        CalculatedQueryLog.CalculatedQueryRead(_logger, dataSetName, read.Count, plan.RowCap);
        var page = Page(request, dataSetName, InMemoryQuery.Apply(read, plan), query.Columns, take);
        page.InMemory = true;
        return GenericResult<DataQueryPage>.Success(page);
    }

    /// <summary>The command a request asks for, with the columns its rows carry: the grouped one, or the plain read of the data set's fields.</summary>
    private static IGenericResult<GroupedQuery> Command(DataQueryTranslator translator, DataQueryRequest request, int take, int rowCap)
    {
        if (DataQueryGrouping.IsRequested(request)) return translator.TranslateGrouped(request, take);

        var plan = translator.Plan(request, take, rowCap);
        return plan.IsFailure
            ? plan.ToNewResult<GroupedQuery>()
            : GenericResult<GroupedQuery>.Success(new GroupedQuery(plan.Value!.Command, translator.Columns(), plan.Value));
    }

    /// <summary>Cuts one page of rows, reading the group count off the first row of a grouped read.</summary>
    private static DataQueryPage Page(
        DataQueryRequest request, string dataSetName, List<Dictionary<string, object?>> rows, IReadOnlyList<DataQueryColumnDto> columns, int take)
        => new()
        {
            DataSetName = dataSetName,
            Columns = columns,
            Rows = [.. rows.Take(take).Select(row => Declared(row, columns))],
            Take = take,
            HasMoreRows = rows.Count > take,
            GroupCount = GroupCount(request, rows),
        };

    /// <summary>
    /// The number of groups in all, which the store carries on every row of a grouped read. A grouped page
    /// with no row has none to read it from, so it is 0 only when nothing was skipped, and unknown otherwise.
    /// </summary>
    private static int? GroupCount(DataQueryRequest request, List<Dictionary<string, object?>> rows)
    {
        if (!DataQueryGrouping.IsRequested(request) || request.GroupBy is not { Count: > 0 }) return null;
        if (rows.Count == 0) return request.Skip == 0 ? 0 : null;

        return rows[0].TryGetValue(AggregationExpression.GroupCountColumn, out var count) && count is not null
            ? Convert.ToInt32(count, CultureInfo.InvariantCulture)
            : null;
    }

    /// <summary>Keeps only the data set's declared fields from a row.</summary>
    /// <remarks>
    /// A Simple set's query selects every column of its container, so a row can carry columns the data
    /// set never declared. The grant is the data set, not the table under it, so those never reach the
    /// caller. A declared field the row does not carry is left out rather than written as null — an
    /// absent value and a null one are different answers.
    /// </remarks>
    private static Dictionary<string, object?> Declared(
        Dictionary<string, object?> row, IReadOnlyList<DataQueryColumnDto> columns)
    {
        var byName = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
        var declared = new Dictionary<string, object?>(columns.Count, StringComparer.Ordinal);
        foreach (var column in columns)
        {
            if (byName.TryGetValue(column.Name, out var value))
                declared[column.Name] = value;
        }

        return declared;
    }
}
