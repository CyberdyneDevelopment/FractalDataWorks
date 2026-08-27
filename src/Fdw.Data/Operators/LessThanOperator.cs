using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Less than operator (&lt;, lt).
/// </summary>
[TypeOption(typeof(FilterOperators), "LessThan", RestrictToCurrentCompilation = true)]
public sealed class LessThanOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOperator"/> class.
    /// </summary>
    public LessThanOperator()
        : base(
            id: 8,
            name: "LessThan",
            sqlOperator: "<",
            odataOperator: "lt",
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
        => FilterValueComparer.Compare(left, right) < 0;
}
