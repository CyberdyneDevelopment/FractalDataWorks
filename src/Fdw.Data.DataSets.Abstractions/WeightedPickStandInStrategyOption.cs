using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Values are drawn from a weighted list of options.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StandInStrategies), "WeightedPick")]
public sealed class WeightedPickStandInStrategyOption : StandInStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="WeightedPickStandInStrategyOption"/> class.</summary>
    public WeightedPickStandInStrategyOption() : base("WeightedPick",
        [
            // Each array element is {value, weight, ordinal} -- StandInParameterSchema describes
            // one request field, not a nested object shape, so the array's own element shape is
            // documented here rather than modelled as further schema entries.
            new StandInParameterSchema { Name = "values", Type = "array", Required = true },
        ])
    {
    }
}
