using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Values are drawn from another data set's real rows — a foreign key that looks like one.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(StandInStrategies), "DrawFromDataSet")]
public sealed class DrawFromDataSetStandInStrategyOption : StandInStrategyBase
{
    /// <summary>Initializes a new instance of the <see cref="DrawFromDataSetStandInStrategyOption"/> class.</summary>
    public DrawFromDataSetStandInStrategyOption() : base("DrawFromDataSet",
        [
            new StandInParameterSchema { Name = "sourceDataSetId", Type = "string", Required = true },
            new StandInParameterSchema { Name = "sourceFieldId", Type = "string", Required = true },
        ],
        limitations: "Preview picks randomly among the distinct values already loaded for the source field's own sample data; it does not execute a live query against the source data set's connection.")
    {
    }
}
