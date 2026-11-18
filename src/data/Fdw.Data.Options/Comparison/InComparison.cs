using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field is one of the values (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "In")]
public sealed class InComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="InComparison"/> class.</summary>
    public InComparison()
        : base("In", 1, null, "Is one of", "The column holds any one of the values you pick.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.In, values);
}
