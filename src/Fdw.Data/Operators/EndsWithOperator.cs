using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// EndsWith operator (LIKE '%value', endswith).
/// A pattern operator: anything may precede the value.
/// </summary>
[TypeOption(typeof(FilterOperators), "EndsWith", RestrictToCurrentCompilation = true)]
public sealed class EndsWithOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndsWithOperator"/> class.
    /// </summary>
    public EndsWithOperator()
        : base(
            name: "EndsWith",
            sqlOperator: "LIKE",
            odataOperator: "endswith",
            requiresValue: true,
            matchesAnythingBefore: true,
            matchesAnythingAfter: false)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value)
    {
        return $"'{value?.ToString()?.Replace("'", "''")}'";
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.AsText(left).EndsWith(FilterValueComparer.AsText(right), StringComparison.OrdinalIgnoreCase);
}
