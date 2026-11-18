using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field is greater than the value (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "GreaterThan")]
public sealed class GreaterThanComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="GreaterThanComparison"/> class.</summary>
    public GreaterThanComparison()
        : base("GreaterThan", 1, 1, "Is greater than", "The column holds more than the value you pick.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.GreaterThan, values[0]);
}
