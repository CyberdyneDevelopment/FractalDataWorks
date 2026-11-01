using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the Aggregate tool: its output holds the group-by fields, typed as on the input, then one field per aggregate.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Aggregate")]
public sealed class AggregateFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="AggregateFieldDeriver"/> class.</summary>
    public AggregateFieldDeriver() : base("Aggregate")
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input)
    {
        if (stage is null)
        {
            return null;
        }

        return
        [
            .. stage.GroupByFields.OrderBy(field => field.Ordinal).Select(field => new GraphField(field.FieldName, TypeOf(input, field.FieldName), OriginOf(input, field.FieldName))),
            .. stage.Aggregations.OrderBy(aggregation => aggregation.ExecutionOrder).Select(aggregation => new GraphField(aggregation.OutputField, null)),
        ];
    }
}
