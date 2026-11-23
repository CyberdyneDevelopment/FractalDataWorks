using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>The component's ContainerName.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TargetSettings), "Container")]
public sealed class ContainerTargetSetting : TargetSettingBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainerTargetSetting"/> class.</summary>
    public ContainerTargetSetting()
        : base("Container", "container")
    {
    }

    /// <inheritdoc />
    public override string? ValueOf(PipelineComponentConfiguration component) => component.ContainerName;
}
