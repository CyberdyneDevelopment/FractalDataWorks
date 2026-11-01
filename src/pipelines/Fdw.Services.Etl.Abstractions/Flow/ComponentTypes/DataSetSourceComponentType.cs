using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Reads the rows of a data set, a page at a time.</summary>
[TypeOption(typeof(ComponentTypes), "DataSetSource")]
public sealed class DataSetSourceComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetSourceComponentType"/> class.</summary>
    public DataSetSourceComponentType() : base(
        "DataSetSource",
        "Data set source",
        "Reads the rows of a data set, a page at a time.",
        "Source",
        [],
        [
            new FlowNodeSpec("output", false, 1, 1, false, "Every row of the data set."),
        ],
        [
            new StageMember("dataSet", "DataSet", true, "The data set to read.", TakesParameterText: true),
            new StageMember("batchSize", "Number", true, "How many rows go in a batch; at least one."),
        ])
    {
    }
}
