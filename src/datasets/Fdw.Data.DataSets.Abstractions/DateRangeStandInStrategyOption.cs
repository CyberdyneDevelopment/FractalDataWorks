using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Values are random dates within a range.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StandInStrategies), "DateRange")]
public sealed class DateRangeStandInStrategyOption : StandInStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="DateRangeStandInStrategyOption"/> class.</summary>
    public DateRangeStandInStrategyOption() : base("DateRange",
        [
            new StandInParameterSchema { Name = "fromDate", Type = "date", Required = true },
            new StandInParameterSchema { Name = "toDate", Type = "date", Required = true },
        ])
    {
    }
}
