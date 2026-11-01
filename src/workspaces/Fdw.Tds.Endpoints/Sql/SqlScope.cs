using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Results;
using Fdw.Protocols.Tds;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql;

/// <summary>
/// What an expression is evaluated against: the request, and the row it is evaluated for, if any.
/// </summary>
/// <remarks>
/// A column reference is bound to its position once per query and reused for every row, so evaluating a
/// large result does not search the columns again for each value.
/// </remarks>
public sealed class SqlScope
{
    private static readonly IReadOnlyDictionary<FunctionCall, object?> NoAggregates = new Dictionary<FunctionCall, object?>();

    private readonly Dictionary<MultiPartIdentifier, int> _bound;
    private readonly int _innerCount;

    /// <summary>Initializes a new instance of the <see cref="SqlScope"/> class.</summary>
    /// <param name="context">The request.</param>
    /// <param name="columns">The columns a row's values are named by; empty when there is no row.</param>
    /// <param name="row">The row, or an empty one when there is no row.</param>
    public SqlScope(SqlExecutionContext context, IReadOnlyList<SqlBoundColumn> columns, object?[] row)
        : this(context, columns, row, new Dictionary<MultiPartIdentifier, int>(ReferenceEqualityComparer.Instance), columns.Count, NoAggregates)
    {
    }

    private SqlScope(
        SqlExecutionContext context,
        IReadOnlyList<SqlBoundColumn> columns,
        object?[] row,
        Dictionary<MultiPartIdentifier, int> bound,
        int innerCount,
        IReadOnlyDictionary<FunctionCall, object?> aggregates)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Row = row ?? throw new ArgumentNullException(nameof(row));
        _bound = bound;
        _innerCount = innerCount;
        Aggregates = aggregates;
    }

    /// <summary>Gets the values of the aggregate calls a grouped query already computed, by the call they answer.</summary>
    public IReadOnlyDictionary<FunctionCall, object?> Aggregates { get; }

    /// <summary>Gets the request.</summary>
    public SqlExecutionContext Context { get; }

    /// <summary>Gets the columns a row's values are named by.</summary>
    public IReadOnlyList<SqlBoundColumn> Columns { get; }

    /// <summary>Gets the row.</summary>
    public object?[] Row { get; }

    /// <summary>A scope over the request alone, with no row.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The scope.</returns>
    public static SqlScope Of(SqlExecutionContext context) => new(context, [], []);

    /// <summary>The same columns and bindings, for another row.</summary>
    /// <param name="row">The row.</param>
    /// <returns>The scope.</returns>
    public SqlScope For(object?[] row) => new(Context, Columns, row, _bound, _innerCount, Aggregates);

    /// <summary>The same row, with the aggregate calls a grouped query computed.</summary>
    /// <param name="aggregates">The aggregate values, by the call they answer.</param>
    /// <returns>The scope.</returns>
    public SqlScope WithAggregates(IReadOnlyDictionary<FunctionCall, object?> aggregates) => new(Context, Columns, Row, _bound, _innerCount, aggregates);

    /// <summary>
    /// A scope for a subquery: its own columns first, then this scope's, so a column the subquery names is
    /// its own when it has one and the outer query's when it does not.
    /// </summary>
    /// <param name="inner">The subquery's own columns.</param>
    /// <returns>The scope, before any inner row; <see cref="Nest"/> gives one per inner row.</returns>
    public SqlScope Enclose(IReadOnlyList<SqlBoundColumn> inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        var columns = inner.Concat(Columns).ToList();
        return new SqlScope(Context, columns, [.. new object?[inner.Count], .. Row],
            new Dictionary<MultiPartIdentifier, int>(ReferenceEqualityComparer.Instance), inner.Count, NoAggregates);
    }

    /// <summary>An enclosed scope for one inner row, keeping the outer row it encloses.</summary>
    /// <param name="innerRow">The subquery's row.</param>
    /// <returns>The scope.</returns>
    public SqlScope Nest(object?[] innerRow)
    {
        ArgumentNullException.ThrowIfNull(innerRow);
        return For([.. innerRow, .. Row[_innerCount..]]);
    }

    /// <summary>Reads the value a column reference names in the current row.</summary>
    /// <param name="reference">The reference as parsed.</param>
    /// <returns>The value, or error 207 for no such column and 209 for one named twice.</returns>
    public IGenericResult<object?> Column(ColumnReferenceExpression reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var identifier = reference.MultiPartIdentifier;
        if (identifier is null)
            return SqlRefusal.Refuse<object?>(SqlServerErrors.NotSupported("*", reference.StartLine), Context.Logger);

        if (!_bound.TryGetValue(identifier, out var index))
        {
            var bound = Bind(identifier, reference.StartLine);
            if (bound.IsFailure)
                return bound.ToNewResult<object?>();

            index = bound.Value;
            _bound[identifier] = index;
        }

        return GenericResult<object?>.Success(Row[index]);
    }

    /// <summary>Finds the position of the column a reference names.</summary>
    /// <param name="identifier">The reference's parts.</param>
    /// <param name="line">The line it was written on.</param>
    /// <returns>The position, or error 207 or 209.</returns>
    public IGenericResult<int> Bind(MultiPartIdentifier identifier, int line)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        var parts = identifier.Identifiers.Select(i => i.Value).ToList();
        var name = parts[^1];
        var qualifier = parts.Count >= 2 ? parts[^2] : null;
        var schema = parts.Count >= 3 ? parts[^3] : null;

        var matches = Enumerable.Range(0, Columns.Count).Where(i => Columns[i].Matches(schema, qualifier, name)).ToList();
        var own = matches.Where(i => i < _innerCount).ToList();
        if (own.Count == 1 || (own.Count == 0 && matches.Count == 1))
            return GenericResult<int>.Success(own.Count == 1 ? own[0] : matches[0]);

        var written = string.Join('.', parts);
        return matches.Count == 0
            ? SqlRefusal.Refuse<int>(SqlServerErrors.InvalidColumnName(written, line), Context.Logger)
            : SqlRefusal.Refuse<int>(new SqlServerMessage(209, 1, 16, $"Ambiguous column name '{written}'.", line), Context.Logger);
    }
}
