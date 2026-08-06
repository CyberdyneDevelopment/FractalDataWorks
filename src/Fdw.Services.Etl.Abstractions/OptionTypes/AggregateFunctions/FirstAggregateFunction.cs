using System.Collections.Generic;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>Returns the first value.</summary>
[TypeOption(typeof(AggregateFunctions), "First")]
public sealed class FirstAggregateFunction : AggregateFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="FirstAggregateFunction"/> class.</summary>
    public FirstAggregateFunction() : base(6, "First")
    {
    }

    /// <inheritdoc/>
    public override object? Apply(IReadOnlyList<object?> values) => values[0];
}
