using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Options;

/// <summary>The kind whose field equals the value (FDW-6340).</summary>
[TypeOption(typeof(RuleComparisons), "Equal")]
public sealed class EqualComparison : RuleComparisonBase
{
    /// <summary>Initializes a new instance of the <see cref="EqualComparison"/> class.</summary>
    public EqualComparison()
        : base("Equal", 1, 1, "Equals", "The column holds the value you pick.")
    {
    }

    /// <inheritdoc />
    public override FilterExpression Append(FilterExpression target, string field, IReadOnlyList<object?> values) =>
        And(target, field, FilterOperators.Equal, values[0]);
}
