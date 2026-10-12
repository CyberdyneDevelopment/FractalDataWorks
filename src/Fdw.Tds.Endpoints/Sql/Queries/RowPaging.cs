using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// How many rows a query skips and returns: OFFSET ... FETCH, TOP, and the session's SET ROWCOUNT.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct RowPaging(long Skip, long? Take)
{
    /// <summary>Reads a query's paging.</summary>
    /// <param name="query">The query as parsed.</param>
    /// <param name="scope">The scope its numbers are evaluated in.</param>
    /// <returns>The paging, or the refusal for TOP PERCENT or WITH TIES.</returns>
    public static IGenericResult<RowPaging> Of(QuerySpecification query, SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(scope);
        var limit = scope.Context.Session.RowCountLimit > 0 ? scope.Context.Session.RowCountLimit : (long?)null;

        if (query.OffsetClause is { } offset)
            return Offset(offset, limit, scope);

        if (query.TopRowFilter is not { } top)
            return GenericResult<RowPaging>.Success(new RowPaging(0, limit));

        if (top.Percent || top.WithTies)
            return SqlRefusal.Refuse<RowPaging>(SqlServerErrors.NotSupported(top.Percent ? "TOP PERCENT" : "TOP WITH TIES", top.StartLine), scope.Context.Logger);

        var count = SqlExpressions.Whole(top.Expression, scope);
        return count.IsFailure ? count.ToNewResult<RowPaging>() : GenericResult<RowPaging>.Success(new RowPaging(0, Smaller(count.Value, limit)));
    }

    /// <summary>Applies the paging to rows held in memory.</summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="rows">The rows, in their final order.</param>
    /// <returns>The rows kept.</returns>
    public IEnumerable<T> Apply<T>(IEnumerable<T> rows)
    {
        var skipped = rows.Skip((int)Math.Min(Skip, int.MaxValue));
        return Take is { } take ? skipped.Take((int)Math.Min(take, int.MaxValue)) : skipped;
    }

    private static IGenericResult<RowPaging> Offset(OffsetClause offset, long? limit, SqlScope scope)
    {
        var skip = SqlExpressions.Whole(offset.OffsetExpression, scope);
        if (skip.IsFailure)
            return skip.ToNewResult<RowPaging>();

        if (offset.FetchExpression is null)
            return GenericResult<RowPaging>.Success(new RowPaging(skip.Value, limit));

        var fetch = SqlExpressions.Whole(offset.FetchExpression, scope);
        return fetch.IsFailure
            ? fetch.ToNewResult<RowPaging>()
            : GenericResult<RowPaging>.Success(new RowPaging(skip.Value, Smaller(fetch.Value, limit)));
    }

    private static long Smaller(long count, long? limit) => limit is { } cap && cap < count ? cap : count;
}
