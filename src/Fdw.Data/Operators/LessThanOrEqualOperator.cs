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

    /// <inheritdoc />
    /// <remarks>
    /// A missing value on either side never matches, as SQL leaves out a row whose value is NULL. The comparer orders a
    /// missing value as equal to anything, which the strict operators already turn away; this one must turn it away itself.
    /// </remarks>
    public override bool Matches(object? left, object? right)
        => left is not null && right is not null && FilterValueComparer.Compare(left, right) <= 0;
}
