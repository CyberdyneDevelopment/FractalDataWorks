using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>Describes the Calculate tool: its output holds the input fields and the calculated fields, in the order the calculations run.</summary>
[TypeOption(typeof(ComponentFieldDerivers), "Calculate")]
public sealed class CalculateFieldDeriver : StageToolFieldDeriverBase
{
    /// <summary>Initializes a new instance of the <see cref="CalculateFieldDeriver"/> class.</summary>
    public CalculateFieldDeriver() : base("Calculate")
    {
    }

    /// <inheritdoc />
    // Why a calculated field of an existing name replaces it in place: the stage writes its result over the field of that name.
    protected override IReadOnlyList<GraphField>? Output(PipelineTransformConfiguration? stage, IReadOnlyList<GraphField>? input)
    {
        if (input is null || stage is null)
        {
            return input;
        }

        var fields = new List<GraphField>(input);
        foreach (var calculation in stage.Calculations.OrderBy(calculation => calculation.ExecutionOrder))
        {
            var added = new GraphField(calculation.OutputField, null);
            var existing = fields.FindIndex(field => string.Equals(field.Name, calculation.OutputField, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
            {
                fields[existing] = added;
            }
            else
            {
                fields.Add(added);
            }
        }

        return fields;
    }
}
