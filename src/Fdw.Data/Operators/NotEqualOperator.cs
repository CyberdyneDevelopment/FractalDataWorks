using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Not equal operator (&lt;&gt;, ne).
/// </summary>
[TypeOption(typeof(FilterOperators), "NotEqual", RestrictToCurrentCompilation = true)]
public sealed class NotEqualOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotEqualOperator"/> class.
    /// </summary>
    public NotEqualOperator()
        : base(
            id: 2,
            name: "NotEqual",
            sqlOperator: "<>",
            odataOperator: "ne",
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
        => !FilterValueComparer.AreEqual(left, right);
}
