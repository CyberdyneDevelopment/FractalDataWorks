using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Averages the numeric values.</summary>
[TypeOption(typeof(AggregateFunctions), "Avg")]
public sealed class AvgAggregateFunction : AggregateFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="AvgAggregateFunction"/> class.</summary>
    public AvgAggregateFunction() : base(3, "Avg")
    {
    }

    /// <inheritdoc/>
    public override object? Apply(IReadOnlyList<object?> values)
        => values.Average(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture));
}
