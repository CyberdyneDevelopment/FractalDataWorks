using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field is less than the value (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "LessThan")]
public sealed class LessThanComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="LessThanComparison"/> class.</summary>
    public LessThanComparison()
        : base("LessThan", 1, 1, "Is less than", "The column holds less than the value you pick.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.LessThan, values[0]);
}
