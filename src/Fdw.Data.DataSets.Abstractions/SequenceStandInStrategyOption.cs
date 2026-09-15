using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Values count up from a start value by a fixed increment.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StandInStrategies), "Sequence")]
public sealed class SequenceStandInStrategyOption : StandInStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="SequenceStandInStrategyOption"/> class.</summary>
    public SequenceStandInStrategyOption() : base("Sequence",
        [
            new StandInParameterSchema { Name = "startValue", Type = "integer", Required = true },
            new StandInParameterSchema { Name = "increment", Type = "integer", Required = true },
        ])
    {
    }
}
