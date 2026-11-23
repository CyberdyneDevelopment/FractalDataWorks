using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>The component's PhysicalPath.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TargetSettings), "PhysicalPath")]
public sealed class PhysicalPathTargetSetting : TargetSettingBase
{
    /// <summary>Initializes a new instance of the <see cref="PhysicalPathTargetSetting"/> class.</summary>
    public PhysicalPathTargetSetting()
        : base("PhysicalPath", "physicalPath")
    {
    }

    /// <inheritdoc />
    public override string? ValueOf(PipelineComponentConfiguration component) => component.PhysicalPath;
}
