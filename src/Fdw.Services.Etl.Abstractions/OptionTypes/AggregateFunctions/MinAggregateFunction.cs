using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Returns the minimum value.</summary>
[TypeOption(typeof(AggregateFunctions), "Min")]
public sealed class MinAggregateFunction : AggregateFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="MinAggregateFunction"/> class.</summary>
    public MinAggregateFunction() : base(4, "Min")
    {
    }

    /// <inheritdoc/>
    public override object? Apply(IReadOnlyList<object?> values) => values.Min();
}
