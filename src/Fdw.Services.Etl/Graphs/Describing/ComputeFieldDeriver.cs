using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the Compute tool: the fields its transformation adds are known only once the transformation is read, so the output's fields are not known here.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Compute")]
public sealed class ComputeFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="ComputeFieldDeriver"/> class.</summary>
    public ComputeFieldDeriver() : base("Compute")
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input) => null;
}
