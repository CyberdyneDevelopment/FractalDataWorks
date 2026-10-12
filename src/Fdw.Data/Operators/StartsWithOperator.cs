using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// StartsWith operator (LIKE 'value%', startswith).
/// A pattern operator: anything may follow the value.
/// </summary>
[TypeOption(typeof(FilterOperators), "StartsWith", RestrictToCurrentCompilation = true)]
public sealed class StartsWithOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartsWithOperator"/> class.
    /// </summary>
    public StartsWithOperator()
        : base(
            name: "StartsWith",
            sqlOperator: "LIKE",
            odataOperator: "startswith",
            requiresValue: true,
            matchesAnythingBefore: false,
            matchesAnythingAfter: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value)
    {
        return $"'{value?.ToString()?.Replace("'", "''")}'";
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.AsText(left).StartsWith(FilterValueComparer.AsText(right), StringComparison.OrdinalIgnoreCase);
}
