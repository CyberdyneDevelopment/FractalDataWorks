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
            name: "LessThan",
            sqlOperator: "<",
            odataOperator: "lt",
            requiresValue: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value) => ODataLiteralKinds.Format(value);

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.Compare(left, right) < 0;
}
