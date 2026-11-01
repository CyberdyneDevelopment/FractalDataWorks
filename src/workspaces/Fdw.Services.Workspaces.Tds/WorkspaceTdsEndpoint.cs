using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Tds.Endpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Tds;

/// <summary>
/// Presents the workspaces the caller may read as SQL Server schemas, and the data sets attached to each as
/// the tables in it.
/// </summary>
/// <remarks>
/// <para>
/// Everything is read as the caller, through <see cref="IWorkspaceReadService"/>: the same service the HTTP
/// query route uses, so a workspace the caller may not read and a data set not attached to it are absent
/// here exactly as they answer 404 there. A table's rows come from the "Main" data gateway against the data
/// set the workspace executes — the variation when it attaches one — as the HTTP route reads them.
/// </para>
/// <para>
/// A column is a current, undeleted field, in the field's own order. A calculated field is a column that
/// cannot be filtered or sorted in the store, because its value exists only after the rows are read.
/// A table pages in the store only when its kind does and it aggregates nothing, which is when the HTTP
/// route accepts paging.
/// </para>
/// <para>
/// A view that stores a filter, or sits under one that does, is not offered (FDW-1032). The HTTP route narrows its
/// rows by those filters; this endpoint runs the table's rows as the statement asks and cannot yet, so offering the
/// view would show rows the view exists to hide. It is absent from the table list and refused by name until the
/// filters can be applied here.
/// </para>
/// </remarks>
public sealed class WorkspaceTdsEndpoint : TdsEndpointBase
{
    private const string GatewayName = "Main";

    private readonly IWorkspaceReadService _workspaces;
    private readonly IDataGatewayProvider _dataGateways;

    /// <summary>Initializes a new instance of the <see cref="WorkspaceTdsEndpoint"/> class.</summary>
    /// <param name="workspaces">Reads workspaces and their data sets as the caller.</param>
    /// <param name="dataGateways">Provides the data gateway rows are read through.</param>
    /// <param name="logger">The logger.</param>
    public WorkspaceTdsEndpoint(IWorkspaceReadService workspaces, IDataGatewayProvider dataGateways, ILogger<WorkspaceTdsEndpoint>? logger)
        : base(logger)
    {
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        _dataGateways = dataGateways ?? throw new ArgumentNullException(nameof(dataGateways));
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IReadOnlyList<string>>> Schemas(CancellationToken cancellationToken = default)
    {
        var readable = await _workspaces.Readable(cancellationToken).ConfigureAwait(false);
        return readable.IsFailure
            ? readable.ToNewResult<IReadOnlyList<string>>()
            : GenericResult<IReadOnlyList<string>>.Success([.. readable.Value!.Select(d => d.Name)]);
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IReadOnlyList<SqlTable>>> Tables(string schema, CancellationToken cancellationToken = default)
    {
        var workspace = await _workspaces.Readable(schema, cancellationToken).ConfigureAwait(false);
        if (workspace.IsFailure)
            return workspace.ToNewResult<IReadOnlyList<SqlTable>>();

        var attached = await _workspaces.DataSets(workspace.Value!, cancellationToken).ConfigureAwait(false);
        return attached.IsFailure
            ? attached.ToNewResult<IReadOnlyList<SqlTable>>()
            : GenericResult<IReadOnlyList<SqlTable>>.Success(
                [.. attached.Value!.Where(d => d.ViewFilters.Count == 0).Select(d => Table(workspace.Value!.Name, d))]);
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<SqlTable>> Table(string schema, string name, CancellationToken cancellationToken = default)
    {
        var attached = await _workspaces.DataSet(schema, name, cancellationToken).ConfigureAwait(false);
        if (attached.IsFailure) return attached.ToNewResult<SqlTable>();

        return attached.Value!.ViewFilters.Count > 0
            ? DataSetVariationChain.Invalid<SqlTable>(
                Logger, name, "it stores a filter, and the filter cannot be applied over TDS yet, so the view is not offered here")
            : GenericResult<SqlTable>.Success(Table(schema, attached.Value!));
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> Rows(
        SqlTable table, QueryCommand<Dictionary<string, object?>> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(command);
        var rows = await _dataGateways.ByName(GatewayName)
            .Execute<IEnumerable<Dictionary<string, object?>>>(command, new DataSetTarget(table.Target), cancellationToken)
            .ConfigureAwait(false);
        if (rows.IsFailure)
            return rows.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        if (rows.Value is null)
        {
            return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Failure(
                DataServiceResultCodes.ByName("DataQueryReturnedNoValue"), Logger, ResultDetails.Create("dataSet", table.Target));
        }

        return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows.Value.ToList());
    }

    private static SqlTable Table(string schema, ReadableDataSet attached)
    {
        var target = attached.ExecutionTarget;
        // Why only these: a field switched off for filtering (FDW-1048) or computed after the rows are read cannot be
        // filtered on through a SQL client, so it is not a column of the table at all.
        var columns = target.Fields
            .Where(f => f.IsCurrent && !f.IsDeleted && f.IsFilterable && !f.IsCalculated)
            .OrderBy(f => f.Ordinal)
            .Select((f, i) => new SqlTableColumn(
                f.Name, i + 1, DataSetFieldTypes.Resolve(f.TypeName), f.TypeName, f.IsNullable, f.MaxLength, f.IsKey, !f.IsCalculated, f.IsFilterable))
            .ToList();
        var pagesInStore = DataSetTypes.ByName(target.Implementation).PagesInStore && target.AggregatesOrNone().Count == 0;
        return new SqlTable(schema, attached.Name, columns, pagesInStore, target.Name, target.CreateDate.LocalDateTime, target.ModifyDate.LocalDateTime);
    }
}
