using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// Greater than or equal operator (&gt;=, ge).
/// </summary>
[TypeOption(typeof(FilterOperators), "GreaterThanOrEqual", RestrictToCurrentCompilation = true)]
public sealed class GreaterThanOrEqualOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterThanOrEqualOperator"/> class.
    /// </summary>
    public GreaterThanOrEqualOperator()
        : base(
            name: "GreaterThanOrEqual",
            sqlOperator: ">=",
            odataOperator: "ge",
            requiresValue: true)
    {
    }

    /// <inheritdoc/>
    public override string FormatODataValue(object? value) => ODataLiteralKinds.Format(value);

    /// <inheritdoc />
    /// <remarks>
    /// A missing value on either side never matches, as SQL leaves out a row whose value is NULL. The comparer orders a
    /// missing value as equal to anything, which the strict operators already turn away; this one must turn it away itself.
    /// </remarks>
    public override bool Matches(object? left, object? right)
        => left is not null && right is not null && FilterValueComparer.Compare(left, right) >= 0;
}
