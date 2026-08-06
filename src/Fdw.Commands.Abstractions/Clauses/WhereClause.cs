using System.Linq.Expressions;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Represents a single WHERE condition extracted from a query.
/// </summary>
public sealed class WhereClause
{
    /// <summary>
    /// Gets the field name being filtered.
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the comparison operator (Equals, NotEquals, GreaterThan, etc.).
    /// </summary>
    public required IFilterOperator Operator { get; init; }

    /// <summary>
    /// Gets the value to compare against.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Gets the logical operator combining this condition with others (And, Or).
    /// </summary>
    public required ILogicalOperator LogicalOperator { get; init; }

    /// <summary>
    /// Gets the original expression for this condition.
    /// Used when translation requires the full expression context.
    /// </summary>
    public Expression OriginalExpression { get; init; } = Expression.Empty();
}
