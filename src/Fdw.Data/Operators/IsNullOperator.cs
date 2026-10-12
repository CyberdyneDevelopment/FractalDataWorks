using System;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>
/// IS NULL operator (IS NULL, eq null).
/// This operator does NOT require a value parameter.
/// </summary>
[TypeOption(typeof(FilterOperators), "IsNull", RestrictToCurrentCompilation = true)]
public sealed class IsNullOperator : FilterOperatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsNullOperator"/> class.
    /// </summary>
    public IsNullOperator()
        : base(
            name: "IsNull",
            sqlOperator: "IS NULL",
            odataOperator: "eq null",
            requiresValue: false)
    {
    }

    /// <summary>
    /// Returns empty string since OData handles "eq null" without formatting.
    /// </summary>
    public override string FormatODataValue(object? value) => string.Empty;

    /// <inheritdoc />
    public override bool Matches(object? left, object? right)
        => left is null;
}
