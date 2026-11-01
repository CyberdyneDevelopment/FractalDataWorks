using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the Validate tool: its output holds the fields of its input.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Validate")]
public sealed class ValidateFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="ValidateFieldDeriver"/> class.</summary>
    public ValidateFieldDeriver() : base("Validate")
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input) => input;
}
