using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field is less than or equal to the value (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "LessThanOrEqual")]
public sealed class LessThanOrEqualComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="LessThanOrEqualComparison"/> class.</summary>
    public LessThanOrEqualComparison()
        : base("LessThanOrEqual", 1, 1, "Is less than or equal to", "The column holds the value you pick or less.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.LessThanOrEqual, values[0]);
}
