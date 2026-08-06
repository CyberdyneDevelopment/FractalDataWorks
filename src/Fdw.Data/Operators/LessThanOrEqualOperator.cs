using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Less than or equal operator (&lt;=, le).
/// </summary>
[TypeOption(typeof(FilterOperators), "LessThanOrEqual", RestrictToCurrentCompilation = true)]
public sealed class LessThanOrEqualOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LessThanOrEqualOperator"/> class.
    /// </summary>
    public LessThanOrEqualOperator()
        : base(
            id: 9,
            name: "LessThanOrEqual",
            sqlOperator: "<=",
            odataOperator: "le",
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
}
