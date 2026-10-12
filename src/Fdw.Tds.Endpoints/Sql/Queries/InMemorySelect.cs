using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Catalog;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Fdw.Tds.Endpoints.Sql.Functions;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Runs queries whose rows are already in memory: the system catalog, a SELECT with no FROM, and every
/// subquery.
/// </summary>
/// <remarks>
/// <para>
/// FROM may join catalog views — INNER and LEFT OUTER joins, comma and CROSS joins, derived tables — which is
/// how SSMS and the drivers read the catalog. A data set's table inside a join or a subquery is refused with
/// error 40517: its rows come from a store, one query at a time, and are read only by the top-level SELECT.
/// </para>
/// </remarks>
public static class InMemorySelect
{
    /// <summary>Runs a query whose sources are in memory.</summary>
    /// <param name="query">The query as parsed.</param>
    /// <param name="outer">The scope it runs in.</param>
    /// <returns>The result, or the refusal.</returns>
    public static IGenericResult<SqlSelectOutput> Run(QuerySpecification query, SqlScope outer)
    {
        ArgumentNullException.ThrowIfNull(query);
        var source = Source(query.FromClause, outer);
        return source.IsFailure
            ? source.ToNewResult<SqlSelectOutput>()
            : SqlProjection.Project(query, source.Value!, outer, ProjectionSteps.All);
    }

    /// <summary>Runs a subquery and answers its rows.</summary>
    /// <param name="query">The subquery as parsed.</param>
    /// <param name="outer">The enclosing query's scope.</param>
    /// <returns>The rows, or the refusal.</returns>
    public static IGenericResult<SqlRowSet> Rows(QueryExpression query, SqlScope outer)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(outer);
        if (query is not QuerySpecification specification)
            return SqlRefusal.Refuse<SqlRowSet>(SqlServerErrors.NotSupported(SqlText.Of(query), query.StartLine), outer.Context.Logger);

        var output = Run(specification, outer);
        if (output.IsFailure)
            return output.ToNewResult<SqlRowSet>();

        var columns = output.Value!.Columns.Select(c => new SqlBoundColumn(string.Empty, string.Empty, c.Name, c.Declared, c.IsNullable)).ToList();
        return GenericResult<SqlRowSet>.Success(new SqlRowSet(columns, output.Value.Rows));
    }

    /// <summary>Runs a subquery that must answer one column, and answers its values.</summary>
    /// <param name="query">The subquery as parsed.</param>
    /// <param name="outer">The enclosing query's scope.</param>
    /// <returns>The values, or error 116 for a subquery of more than one column.</returns>
    public static IGenericResult<IReadOnlyList<object?>> Column(QueryExpression query, SqlScope outer)
    {
        var rows = Rows(query, outer);
        if (rows.IsFailure)
            return rows.ToNewResult<IReadOnlyList<object?>>();

        if (rows.Value!.Columns.Count != 1)
        {
            return SqlRefusal.Refuse<IReadOnlyList<object?>>(new SqlServerMessage(116, 1, 16,
                "Only one expression can be specified in the select list when the subquery is not introduced with EXISTS.", query.StartLine), outer.Context.Logger);
        }

        return GenericResult<IReadOnlyList<object?>>.Success(rows.Value.Rows.Select(r => r[0]).ToList());
    }

    /// <summary>Runs a scalar subquery: NULL for no row, its value for one, error 512 for more.</summary>
    /// <param name="subquery">The subquery as parsed.</param>
    /// <param name="outer">The enclosing query's scope.</param>
    /// <returns>The value, or the refusal.</returns>
    public static IGenericResult<object?> Scalar(ScalarSubquery subquery, SqlScope outer)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        var values = Column(subquery.QueryExpression, outer);
        if (values.IsFailure)
            return values.ToNewResult<object?>();

        return values.Value!.Count switch
        {
            0 => GenericResult<object?>.Success(null),
            1 => GenericResult<object?>.Success(values.Value[0]),
            _ => SqlRefusal.Refuse<object?>(new SqlServerMessage(512, 1, 16,
                "Subquery returned more than 1 value. This is not permitted when the subquery follows =, !=, <, <= , >, >= or when the subquery is used as an expression.",
                subquery.StartLine), outer.Context.Logger),
        };
    }

    /// <summary>Reads a FROM clause whose sources are all in memory.</summary>
    /// <param name="from">The clause, or null for a SELECT without one.</param>
    /// <param name="outer">The scope it runs in.</param>
    /// <returns>The rows, or the refusal.</returns>
    public static IGenericResult<SqlRowSet> Source(FromClause? from, SqlScope outer)
    {
        ArgumentNullException.ThrowIfNull(outer);
        if (from is null || from.TableReferences.Count == 0)
            return GenericResult<SqlRowSet>.Success(SqlRowSet.SingleEmptyRow());

        var joined = Reference(from.TableReferences[0], outer);
        foreach (var next in from.TableReferences.Skip(1))
        {
            if (joined.IsFailure)
                return joined;

            var right = Reference(next, outer);
            if (right.IsFailure)
                return right;

            joined = GenericResult<SqlRowSet>.Success(SqlRowSet.Join(joined.Value!, right.Value!, keep: null, leftOuter: false));
        }

        return joined;
    }

    private static IGenericResult<SqlRowSet> Reference(TableReference reference, SqlScope outer) => reference switch
    {
        NamedTableReference named => Named(named, outer),
        QualifiedJoin join => Qualified(join, outer),
        UnqualifiedJoin cross when cross.UnqualifiedJoinType == UnqualifiedJoinType.CrossJoin => Cross(cross, outer),
        QueryDerivedTable derived => Derived(derived, outer),
        JoinParenthesisTableReference parenthesis => Reference(parenthesis.Join, outer),
        _ => SqlRefusal.Refuse<SqlRowSet>(SqlServerErrors.NotSupported(SqlText.Of(reference), reference.StartLine), outer.Context.Logger),
    };

    private static IGenericResult<SqlRowSet> Named(NamedTableReference named, SqlScope outer)
    {
        var name = named.SchemaObject;
        var schema = name.SchemaIdentifier?.Value;
        var context = outer.Context;

        if (schema is null || !CatalogViews.IsCatalogSchema(schema) || !Databases.IsKnown(name.DatabaseIdentifier?.Value, context))
            return NotInMemory(named, outer);

        if (!CatalogViews.TryFind(schema, name.BaseIdentifier.Value, out var view))
            return SqlRefusal.Refuse<SqlRowSet>(SqlServerErrors.InvalidObjectName(SqlText.Of(name), named.StartLine), context.Logger);

        if (context.LoadedCatalog is not { } catalog)
        {
            return SqlRefusal.Refuse<SqlRowSet>(new SqlServerMessage(50000, 1, 16,
                "The catalog was not read before a statement that reads it.", named.StartLine), context.Logger);
        }

        var qualifier = named.Alias?.Value is { } alias ? alias : name.BaseIdentifier.Value;
        return GenericResult<SqlRowSet>.Success(CatalogViews.Read(view, qualifier, context, catalog));
    }

    // A table that is not a catalog view is a data set's, or does not exist.
    private static IGenericResult<SqlRowSet> NotInMemory(NamedTableReference named, SqlScope outer)
        => SqlRefusal.Refuse<SqlRowSet>(
            SqlServerErrors.NotSupported($"{SqlText.Of(named.SchemaObject)} in a join or subquery", named.StartLine), outer.Context.Logger);

    private static IGenericResult<SqlRowSet> Qualified(QualifiedJoin join, SqlScope outer)
    {
        var type = join.QualifiedJoinType;
        if (type is not (QualifiedJoinType.Inner or QualifiedJoinType.LeftOuter))
            return SqlRefusal.Refuse<SqlRowSet>(SqlServerErrors.NotSupported($"{type} JOIN", join.StartLine), outer.Context.Logger);

        var left = Reference(join.FirstTableReference, outer);
        if (left.IsFailure)
            return left;

        var right = Reference(join.SecondTableReference, outer);
        if (right.IsFailure)
            return right;

        var columns = left.Value!.Columns.Concat(right.Value!.Columns).ToList();
        var scope = outer.Enclose(columns);
        IGenericResult? failure = null;
        var joined = SqlRowSet.Join(left.Value, right.Value, row =>
        {
            var holds = SqlPredicates.Evaluate(join.SearchCondition, scope.Nest(row));
            if (holds.IsSuccess)
                return holds.Value == true;

            // The first failure is the join's answer; the rows after it are not kept.
            if (failure is null)
                failure = holds;

            return false;
        }, leftOuter: type == QualifiedJoinType.LeftOuter);

        return failure is null ? GenericResult<SqlRowSet>.Success(joined) : failure.ToNewResult<SqlRowSet>();
    }

    private static IGenericResult<SqlRowSet> Cross(UnqualifiedJoin cross, SqlScope outer)
    {
        var left = Reference(cross.FirstTableReference, outer);
        if (left.IsFailure)
            return left;

        var right = Reference(cross.SecondTableReference, outer);
        return right.IsFailure ? right : GenericResult<SqlRowSet>.Success(SqlRowSet.Join(left.Value!, right.Value!, keep: null, leftOuter: false));
    }

    private static IGenericResult<SqlRowSet> Derived(QueryDerivedTable derived, SqlScope outer)
    {
        var rows = Rows(derived.QueryExpression, outer);
        if (rows.IsFailure)
            return rows;

        var alias = derived.Alias?.Value is { } name ? name : string.Empty;
        var columns = rows.Value!.Columns.Select(c => new SqlBoundColumn(alias, string.Empty, c.Name, c.Type, c.IsNullable)).ToList();
        return GenericResult<SqlRowSet>.Success(new SqlRowSet(columns, rows.Value.Rows));
    }
}
