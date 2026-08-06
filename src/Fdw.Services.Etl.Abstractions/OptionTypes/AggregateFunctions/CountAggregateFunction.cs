using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Counts the values.</summary>
[TypeOption(typeof(AggregateFunctions), "Count")]
public sealed class CountAggregateFunction : AggregateFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="CountAggregateFunction"/> class.</summary>
    public CountAggregateFunction() : base(2, "Count")
    {
    }

    /// <inheritdoc/>
    public override object? Apply(IReadOnlyList<object?> values) => values.Count;
}
