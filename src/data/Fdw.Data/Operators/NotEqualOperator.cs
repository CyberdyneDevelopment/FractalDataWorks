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
            name: "NotEqual",
            sqlOperator: "<>",
            odataOperator: "ne",
            requiresValue: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value) => ODataLiteralKinds.Format(value);

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => !FilterValueComparer.AreEqual(left, right);
}
