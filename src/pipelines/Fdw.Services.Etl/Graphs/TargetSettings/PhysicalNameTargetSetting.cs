using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>The component's PhysicalName.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TargetSettings), "PhysicalName")]
public sealed class PhysicalNameTargetSetting : TargetSettingBase
{
    /// <summary>Initializes a new instance of the <see cref="PhysicalNameTargetSetting"/> class.</summary>
    public PhysicalNameTargetSetting()
        : base("PhysicalName", "physicalName")
    {
    }

    /// <inheritdoc />
    public override string? ValueOf(PipelineComponentConfiguration component) => component.PhysicalName;
}
