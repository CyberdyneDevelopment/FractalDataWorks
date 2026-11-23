using System;
using System.Collections;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// IN operator (IN (...), in).
/// Value should be an IEnumerable of values.
/// </summary>
[TypeOption(typeof(FilterOperators), "In", RestrictToCurrentCompilation = true)]
public sealed class InOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InOperator"/> class.
    /// </summary>
    public InOperator()
        : base(
            name: "In",
            sqlOperator: "IN",
            odataOperator: "in",
            requiresValue: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value)
    {
        if (value == null)
            return "()";

        if (value is IEnumerable enumerable and not string)
        {
            var values = enumerable.Cast<object>().Select(ODataLiteralKinds.Format);

            return $"({string.Join(",", values)})";
        }

        return $"('{value}')";
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.IsIn(left, right);
}
