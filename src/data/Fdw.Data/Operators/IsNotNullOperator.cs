using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// IS NOT NULL operator (IS NOT NULL, ne null).
/// This operator does NOT require a value parameter.
/// </summary>
[TypeOption(typeof(FilterOperators), "IsNotNull", RestrictToCurrentCompilation = true)]
public sealed class IsNotNullOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsNotNullOperator"/> class.
    /// </summary>
    public IsNotNullOperator()
        : base(
            name: "IsNotNull",
            sqlOperator: "IS NOT NULL",
            odataOperator: "ne null",
            requiresValue: false)
    {
    }

    /// <summary>
    /// Returns empty string since OData handles "ne null" without formatting.
    /// </summary>
    public override string FormatODataValue(object? value) => string.Empty;

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => left is not null;
}
