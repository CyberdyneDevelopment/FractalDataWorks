using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the Sort tool: its output holds the fields of its input.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Sort")]
public sealed class SortFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="SortFieldDeriver"/> class.</summary>
    public SortFieldDeriver() : base("Sort")
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input) => input;
}
