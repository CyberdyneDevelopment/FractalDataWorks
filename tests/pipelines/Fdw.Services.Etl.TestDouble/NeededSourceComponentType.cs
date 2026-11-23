using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.TestDouble;

/// <summary>A source component type from another package (FDW-6240), declared in this test double, an assembly its consumers reference, because the module initializer collects options from referenced assemblies only (FDW-6244); its builder needs a contribution.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ComponentTypes), "NeededSource")]
public sealed class NeededSourceComponentType : ComponentTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NeededSourceComponentType"/> class.</summary>
    public NeededSourceComponentType() : base(
        "NeededSource",
        "Needed source",
        "Reads a data set, with a service of its package's own.",
        "Source",
        [],
        [new FlowNodeSpec("output", false, 1, 1, false, "Every row of the data set.")],
        [
            new StageMember("dataSet", "DataSet", true, "The data set to read."),
            new StageMember("batchSize", "Number", true, "How many rows go in a batch."),
        ])
    {
    }
}
