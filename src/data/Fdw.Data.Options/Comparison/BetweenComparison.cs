using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field lies from the first value to the second, both included (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "Between")]
public sealed class BetweenComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="BetweenComparison"/> class.</summary>
    public BetweenComparison()
        : base("Between", 2, 2, "Is between", "The column holds a value from the low value you pick up to the high value you pick, both included.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(And(target, field, FilterOperators.GreaterThanOrEqual, values[0]), field, FilterOperators.LessThanOrEqual, values[1]);
}
