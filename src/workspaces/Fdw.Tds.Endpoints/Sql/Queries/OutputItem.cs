using System;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// One column of a query's select list, and how its value is read from a row.
/// </summary>
internal sealed class OutputItem
{
    private readonly ScalarExpression? _expression;
    private readonly int _sourceIndex;

    private OutputItem(SqlOutputColumn column, ScalarExpression? expression, int sourceIndex)
    {
        Column = column;
        _expression = expression;
        _sourceIndex = sourceIndex;
    }

    /// <summary>Gets the result column.</summary>
    public SqlOutputColumn Column { get; }

    /// <summary>A column a star expands to: read straight from the row.</summary>
    /// <param name="column">The result column.</param>
    /// <param name="sourceIndex">Its position in the row.</param>
    /// <returns>The item.</returns>
    public static OutputItem FromRow(SqlOutputColumn column, int sourceIndex) => new(column, null, sourceIndex);

    /// <summary>A column an expression computes.</summary>
    /// <param name="column">The result column.</param>
    /// <param name="expression">The expression.</param>
    /// <returns>The item.</returns>
    public static OutputItem FromExpression(SqlOutputColumn column, ScalarExpression expression) => new(column, expression, -1);

    /// <summary>Reads the column's value for a row.</summary>
    /// <param name="scope">The row's scope.</param>
    /// <returns>The value, or the refusal.</returns>
    public IGenericResult<object?> Read(SqlScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return _expression is null
            ? GenericResult<object?>.Success(scope.Row[_sourceIndex])
            : SqlExpressions.Evaluate(_expression, scope);
    }
}
