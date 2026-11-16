using System;
using System.Collections;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Matches a value that is none of a list of values (SQL <c>NOT IN</c>, FDW-5666): the counterpart of <c>In</c>, so a caller can
/// leave a list of values out of a read.
/// </summary>
/// <remarks>
/// A SQL translator renders it through the same list path as <c>In</c>: each value a parameter, or, above the connection's
/// setting, one key-list table (FDW-4267). An empty list leaves every non-null value in.
/// </remarks>
[TypeOption(typeof(FilterOperators), "NotIn", RestrictToCurrentCompilation = true)]
public sealed class NotInOperator : FilterOperatorBase
{
    /// <summary>Initializes a new instance of the <see cref="NotInOperator"/> class.</summary>
    public NotInOperator()
        : base(
            name: "NotIn",
            sqlOperator: "NOT IN",
            odataOperator: "not in",
            requiresValue: true)
    {
    }

    /// <inheritdoc />
    public override string FormatODataValue(object? value)
    {
        if (value == null)
            return "()";

        if (value is IEnumerable enumerable and not string)
        {
            var values = enumerable.Cast<object>()
                .Select(v => v switch
                {
                    string str => $"'{str.Replace("'", "''", StringComparison.Ordinal)}'",
                    int or long or short or byte => v.ToString(),
                    decimal or double or float => v.ToString(),
                    _ => $"'{v}'"
                });

            return $"({string.Join(",", values)})";
        }

        return $"('{value}')";
    }

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => !FilterValueComparer.IsIn(left, right);
}
