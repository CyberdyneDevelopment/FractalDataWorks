using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Greater than operator (&gt;, gt).
/// </summary>
[TypeOption(typeof(FilterOperators), "GreaterThan", RestrictToCurrentCompilation = true)]
public sealed class GreaterThanOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOperator"/> class.
    /// </summary>
    public GreaterThanOperator()
        : base(
            name: "GreaterThan",
            sqlOperator: ">",
            odataOperator: "gt",
            requiresValue: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value) => ODataLiteralKinds.Format(value);

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => FilterValueComparer.Compare(left, right) > 0;
}
