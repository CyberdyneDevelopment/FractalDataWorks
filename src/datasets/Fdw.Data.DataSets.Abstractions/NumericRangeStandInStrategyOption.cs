using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Values are random numbers within a range.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StandInStrategies), "NumericRange")]
public sealed class NumericRangeStandInStrategyOption : StandInStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="NumericRangeStandInStrategyOption"/> class.</summary>
    public NumericRangeStandInStrategyOption() : base("NumericRange",
        [
            new StandInParameterSchema { Name = "minValue", Type = "number", Required = true },
            new StandInParameterSchema { Name = "maxValue", Type = "number", Required = true },
            new StandInParameterSchema { Name = "decimals", Type = "integer", Required = false },
        ])
    {
    }
}
