using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Equal operator (=, eq).
/// No switch statements needed - operator knows its own representations!
/// </summary>
[TypeOption(typeof(FilterOperators), "Equal", RestrictToCurrentCompilation = true)]
public sealed class EqualOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EqualOperator"/> class.
    /// </summary>
    public EqualOperator()
        : base(
            id: 1,
            name: "Equal",
            sqlOperator: "=",
            odataOperator: "eq",
            requiresValue: true)
    {
    }

    /// <summary>
    /// Formats the value for OData query strings.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted OData value string.</returns>
    public override string FormatODataValue(object? value)
    {
        if (value == null)
            return "null";

        return value switch
        {
            string str => $"'{str.Replace("'", "''")}'",
            int or long or short or byte => value.ToString()!,
            decimal or double or float => value.ToString()!,
            bool b => b.ToString().ToLowerInvariant(),
            DateTime dt => $"datetime'{dt:yyyy-MM-ddTHH:mm:ss}'",
            DateTimeOffset dto => $"datetimeoffset'{dto:yyyy-MM-ddTHH:mm:sszzz}'",
            Guid guid => $"guid'{guid}'",
            _ => $"'{value}'"
        };
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.AreEqual(left, right);
}
