using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field is greater than or equal to the value (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "GreaterThanOrEqual")]
public sealed class GreaterThanOrEqualComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterThanOrEqualComparison"/> class.</summary>
    public GreaterThanOrEqualComparison()
        : base("GreaterThanOrEqual", 1, 1, "Is greater than or equal to", "The column holds the value you pick or more.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.GreaterThanOrEqual, values[0]);
}
