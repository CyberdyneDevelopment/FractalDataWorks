using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Results;
using Fdw.Tds.Endpoints.Logging;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Runs a SELECT of one data set's table: resolved for the caller, filtered, ordered and paged in its store,
/// then projected over the rows it returns.
/// </summary>
/// <remarks>
/// A table the caller may not read is answered as one that does not exist, error 208. A row the store returns
/// without one of the table's declared columns is a fault in the data set, refused with the column's name
/// rather than written as NULL: an absent value and a NULL one are different answers.
/// </remarks>
public static class DataTableSelect
{
    /// <summary>Runs the query.</summary>
    /// <param name="query">The query as parsed.</param>
    /// <param name="named">The table its FROM names.</param>
    /// <param name="scope">The scope it runs in.</param>
    /// <param name="cancellationToken">A token to cancel the query.</param>
    /// <returns>The result, or the refusal.</returns>
    public static async Task<IGenericResult<SqlSelectOutput>> Run(QuerySpecification query, NamedTableReference named, SqlScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(named);
        ArgumentNullException.ThrowIfNull(scope);

        var table = await Resolve(named, scope, cancellationToken).ConfigureAwait(false);
        if (table.IsFailure)
            return table.ToNewResult<SqlSelectOutput>();

        var qualifier = named.Alias?.Value is { } alias ? alias : table.Value!.Name;
        var columns = Bound(table.Value!, qualifier, scope);
        var plan = PushdownPlanner.Plan(query, table.Value!, qualifier, scope);
        if (plan.IsFailure)
            return plan.ToNewResult<SqlSelectOutput>();

        if (scope.Context.Session.FormatOnly)
            return FormatOnly(query, table.Value!, columns, scope);

        var context = scope.Context;
        TdsEndpointLog.QueryingTable(context.Logger, table.Value!.Schema, table.Value.Name,
            plan.Value!.Command.Filter is not null, plan.Value.Command.Paging is not null);
        var rows = await context.Endpoint.Rows(table.Value, plan.Value.Command, cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
            return rows.ToNewResult<SqlSelectOutput>();

        var rowSet = RowSet(table.Value, columns, rows.Value!, scope);
        return rowSet.IsFailure
            ? rowSet.ToNewResult<SqlSelectOutput>()
            : SqlProjection.Project(query, rowSet.Value!, scope, plan.Value.Steps);
    }

    private static async Task<IGenericResult<SqlTable>> Resolve(NamedTableReference named, SqlScope scope, CancellationToken cancellationToken)
    {
        var name = named.SchemaObject;
        var context = scope.Context;
        var ours = name.DatabaseIdentifier is null
            || string.Equals(name.DatabaseIdentifier.Value, context.Server.DatabaseName, StringComparison.OrdinalIgnoreCase);

        // No default schema holds a table here, so an unqualified name names none.
        if (name.ServerIdentifier is not null || !ours || name.SchemaIdentifier is null)
            return NotFound(named, scope);

        var table = await context.Endpoint.Table(name.SchemaIdentifier.Value, name.BaseIdentifier.Value, cancellationToken).ConfigureAwait(false);
        if (table.IsFailure)
            return IsMissing(table) ? NotFound(named, scope) : table;

        return table.Value is null ? NotFound(named, scope) : table;
    }

    private static bool IsMissing(IGenericResult result)
        => result.Code is { } code && ReferenceEquals(ResultCategories.ById(code.Id / 10000), ResultCategories.Missing);

    private static IGenericResult<SqlTable> NotFound(NamedTableReference named, SqlScope scope)
        => SqlRefusal.Refuse<SqlTable>(SqlServerErrors.InvalidObjectName(SqlText.Of(named.SchemaObject), named.StartLine), scope.Context.Logger);

    private static List<SqlBoundColumn> Bound(SqlTable table, string qualifier, SqlScope scope)
        => table.Columns.Select(c => new SqlBoundColumn(qualifier, table.Schema, c.Name, DeclaredType(c, scope), c.IsNullable)).ToList();

    // A column whose declared type has no SQL Server equivalent is typed from its values when the result is written;
    // SqlColumnTyping has already logged why.
    private static TdsColumnType? DeclaredType(SqlTableColumn column, SqlScope scope)
    {
        var declared = SqlColumnTyping.Declared(column, scope.Context.Logger);
        if (declared.IsFailure)
            return null;

        return declared.Value;
    }

    // SET FMTONLY ON: the columns, no rows, nothing read. A decimal whose precision only its values show
    // cannot be described without them, so it is refused rather than given a precision nobody declared.
    private static IGenericResult<SqlSelectOutput> FormatOnly(QuerySpecification query, SqlTable table, List<SqlBoundColumn> columns, SqlScope scope)
    {
        if (columns.FirstOrDefault(c => c.Type is { DataType.HasPrecision: true, Precision: 0 }) is { } undeclared)
        {
            return SqlRefusal.Refuse<SqlSelectOutput>(new SqlServerMessage(50000, 5, 16,
                $"Column '{undeclared.Name}' of {table} declares no precision or scale, so it cannot be described without reading its values.", query.StartLine),
                scope.Context.Logger);
        }

        return SqlProjection.Project(query, new SqlRowSet(columns, []), scope, ProjectionSteps.All);
    }

    private static IGenericResult<SqlRowSet> RowSet(
        SqlTable table, List<SqlBoundColumn> columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, SqlScope scope)
    {
        var values = new List<object?[]>(rows.Count);
        foreach (var row in rows)
        {
            var byName = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);
            var ordered = new object?[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                if (!byName.TryGetValue(columns[i].Name, out var value))
                {
                    return SqlRefusal.Refuse<SqlRowSet>(new SqlServerMessage(50000, 6, 16,
                        $"A row of {table} came back without its declared column '{columns[i].Name}'.", 1), scope.Context.Logger);
                }

                ordered[i] = value;
            }

            values.Add(ordered);
        }

        return GenericResult<SqlRowSet>.Success(new SqlRowSet(columns, values));
    }
}
