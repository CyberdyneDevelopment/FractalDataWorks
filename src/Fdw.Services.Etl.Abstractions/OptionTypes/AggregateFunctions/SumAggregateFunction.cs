using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Sums the numeric values.</summary>
[TypeOption(typeof(AggregateFunctions), "Sum")]
public sealed class SumAggregateFunction : AggregateFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SumAggregateFunction"/> class.</summary>
    public SumAggregateFunction() : base(1, "Sum")
    {
    }

    /// <inheritdoc/>
    public override object? Apply(IReadOnlyList<object?> values)
        => values.Sum(v => Convert.ToDecimal(v, CultureInfo.InvariantCulture));
}
