using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the Filter tool: its output holds the fields of its input.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Filter")]
public sealed class FilterFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="FilterFieldDeriver"/> class.</summary>
    public FilterFieldDeriver() : base("Filter")
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input) => input;
}
