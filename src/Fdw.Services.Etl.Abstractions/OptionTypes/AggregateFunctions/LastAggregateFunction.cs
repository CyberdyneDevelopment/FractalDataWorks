using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Returns the last value.</summary>
[TypeOption(typeof(AggregateFunctions), "Last")]
public sealed class LastAggregateFunction : AggregateFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="LastAggregateFunction"/> class.</summary>
    public LastAggregateFunction() : base(7, "Last")
    {
    }

    /// <inheritdoc/>
    public override object? Apply(IReadOnlyList<object?> values) => values[^1];
}
