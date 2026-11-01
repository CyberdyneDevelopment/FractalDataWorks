using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the Distinct tool: its output holds the fields of its input.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Distinct")]
public sealed class DistinctFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="DistinctFieldDeriver"/> class.</summary>
    public DistinctFieldDeriver() : base("Distinct")
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input) => input;
}
