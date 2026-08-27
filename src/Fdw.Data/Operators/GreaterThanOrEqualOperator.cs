using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Greater than or equal operator (&gt;=, ge).
/// </summary>
[TypeOption(typeof(FilterOperators), "GreaterThanOrEqual", RestrictToCurrentCompilation = true)]
public sealed class GreaterThanOrEqualOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualOperator"/> class.
    /// </summary>
    public GreaterThanOrEqualOperator()
        : base(
            id: 7,
            name: "GreaterThanOrEqual",
            sqlOperator: ">=",
            odataOperator: "ge",
            requiresValue: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value)
    {
        if (value == null)
            return "null";

        return value switch
        {
            int or long or short or byte => value.ToString()!,
            decimal or double or float => value.ToString()!,
            DateTime dt => $"datetime'{dt:yyyy-MM-ddTHH:mm:ss}'",
            DateTimeOffset dto => $"datetimeoffset'{dto:yyyy-MM-ddTHH:mm:sszzz}'",
            _ => $"'{value}'"
        };
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.Compare(left, right) >= 0;
}
