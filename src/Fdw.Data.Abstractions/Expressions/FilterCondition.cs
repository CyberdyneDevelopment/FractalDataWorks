using System;
using System.Collections;
using System.Linq;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a single filter condition (property, operator, value).
/// </summary>
/// <remarks>
/// <para>
/// Filter conditions use FilterOperatorBase instead of enums, eliminating switch statements.
/// Each operator knows its own SQL and OData representations.
/// </para>
/// <para>
/// Example:
/// <code>
/// var condition = new FilterCondition
/// {
///     PropertyName = "Name",
///     Operator = FilterOperators.Contains,  // No enum, no switch!
///     Value = "Acme"
/// };
///
/// // Direct property access - no switch statements
/// var sqlCondition = $"[{condition.PropertyName}] {condition.Operator.SqlOperator} {condition.Operator.FormatSqlParameter(condition.PropertyName)}";
/// </code>
/// </para>
/// </remarks>
public sealed record FilterCondition : IFilterCondition, IFilterNode
{
    /// <summary>
    /// Gets the property name to filter on.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Gets the filter operator.
    /// This is a FilterOperatorBase (TypeCollection), not an enum!
    /// </summary>
    public required IFilterOperator Operator { get; init; }

    /// <summary>
    /// Gets the value to compare against (null for IS NULL / IS NOT NULL operators).
    /// </summary>
    public object? Value { get; init; }

    // Why: the compiler-synthesized record Equals/GetHashCode compare Value via
    // EqualityComparer<object>.Default, which is value-correct for scalars but falls back to reference
    // identity for a collection — e.g. FilterConditionBuilder.In(...) sets Value to the caller's
    // IEnumerable. Two structurally-identical IN-clause conditions built fresh per call (the normal
    // case) would otherwise hash/compare unequal, the same defect fixed on FilterGroup.Nodes, and for
    // the same reason: CacheKeyBuilder.ComputeCacheKey needs GetHashCode() to be value-based to key the
    // DataGateway result cache.
    /// <inheritdoc/>
    public bool Equals(FilterCondition? other) =>
        other is not null
        && string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal)
        && Equals(Operator, other.Operator)
        && ValueEquals(Value, other.Value);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(PropertyName, StringComparer.Ordinal);
        hash.Add(Operator);
        hash.Add(ValueHashCode(Value));
        return hash.ToHashCode();
    }

    private static bool ValueEquals(object? left, object? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left is not string && left is IEnumerable leftItems
            && right is not string && right is IEnumerable rightItems)
            return leftItems.Cast<object?>().SequenceEqual(rightItems.Cast<object?>());
        return left.Equals(right);
    }

    private static int ValueHashCode(object? value)
    {
        if (value is null) return 0;
        if (value is not string && value is IEnumerable items)
        {
            var hash = new HashCode();
            foreach (var item in items)
                hash.Add(item);
            return hash.ToHashCode();
        }

        return value.GetHashCode();
    }
}
