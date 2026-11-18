using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field does not equal the value (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "NotEqual")]
public sealed class NotEqualComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="NotEqualComparison"/> class.</summary>
    public NotEqualComparison()
        : base("NotEqual", 1, 1, "Does not equal", "The column does not hold the value you pick.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.NotEqual, values[0]);
}
