using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Every generated value is the same literal.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StandInStrategies), "FixedValue")]
public sealed class FixedValueStandInStrategyOption : StandInStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="FixedValueStandInStrategyOption"/> class.</summary>
    public FixedValueStandInStrategyOption() : base("FixedValue",
        [
            new StandInParameterSchema { Name = "fixedValue", Type = "string", Required = true },
        ])
    {
    }
}
