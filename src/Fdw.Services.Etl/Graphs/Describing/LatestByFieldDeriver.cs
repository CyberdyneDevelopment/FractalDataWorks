using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the LatestBy tool: its output holds the fields of its input.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "LatestBy")]
public sealed class LatestByFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="LatestByFieldDeriver"/> class.</summary>
    public LatestByFieldDeriver() : base("LatestBy")
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input) => input;
}
