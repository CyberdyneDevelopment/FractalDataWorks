using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Contains operator (LIKE '%value%', contains).
/// A pattern operator: anything may precede and follow the value.
/// </summary>
[TypeOption(typeof(FilterOperators), "Contains", RestrictToCurrentCompilation = true)]
public sealed class ContainsOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainsOperator"/> class.
    /// </summary>
    public ContainsOperator()
        : base(
            name: "Contains",
            sqlOperator: "LIKE",
            odataOperator: "contains",
            requiresValue: true,
            matchesAnythingBefore: true,
            matchesAnythingAfter: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value)
    {
        return value switch
        {
            string str => $"'{str.Replace("'", "''")}'",
            _ => $"'{value}'"
        };
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.AsText(left).Contains(FilterValueComparer.AsText(right), StringComparison.OrdinalIgnoreCase);
}
