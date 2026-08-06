using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Returns the maximum value.</summary>
[TypeOption(typeof(AggregateFunctions), "Max")]
public sealed class MaxAggregateFunction : AggregateFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="MaxAggregateFunction"/> class.</summary>
    public MaxAggregateFunction() : base(5, "Max")
    {
    }

    /// <inheritdoc/>
    public override object? Apply(IReadOnlyList<object?> values) => values.Max();
}
