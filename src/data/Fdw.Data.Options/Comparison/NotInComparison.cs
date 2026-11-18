using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field is none of the values (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "NotIn")]
public sealed class NotInComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="NotInComparison"/> class.</summary>
    public NotInComparison()
        : base("NotIn", 1, null, "Is not one of", "The column holds none of the values you pick.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.NotIn, values);
}
