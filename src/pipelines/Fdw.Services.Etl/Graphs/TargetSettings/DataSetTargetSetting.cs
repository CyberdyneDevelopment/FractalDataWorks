using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>The component's DataSetName.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TargetSettings), "DataSet")]
public sealed class DataSetTargetSetting : TargetSettingBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetTargetSetting"/> class.</summary>
    public DataSetTargetSetting()
        : base("DataSet", "dataSet")
    {
    }

    /// <inheritdoc />
    public override string? ValueOf(PipelineComponentConfiguration component) => component.DataSetName;
}
