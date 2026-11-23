using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>The component's SplitField.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TargetSettings), "SplitField")]
public sealed class SplitFieldTargetSetting : TargetSettingBase
{
    /// <summary>Initializes a new instance of the <see cref="SplitFieldTargetSetting"/> class.</summary>
    public SplitFieldTargetSetting()
        : base("SplitField", "splitField")
    {
    }

    /// <inheritdoc />
    public override string? ValueOf(PipelineComponentConfiguration component) => component.SplitField;
}
