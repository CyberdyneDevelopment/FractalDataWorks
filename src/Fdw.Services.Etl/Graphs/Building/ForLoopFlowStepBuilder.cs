using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a for loop: its steps run once for each value of a counter.</summary>
[TypeOption(typeof(FlowStepBuilders), "ForLoop")]
public sealed class ForLoopFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ForLoopFlowStepBuilder"/> class.</summary>
    public ForLoopFlowStepBuilder() : base("ForLoop")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.VariableName is not { } variable || component.StartValue is not { } start || component.EndValue is not { } end || component.StepValue is not { } by)
        {
            return Missing(component, context);
        }

        return GenericResult<IFlowStep>.Success(new FlowForLoop(component.Name, variable, start, end, by, body));
    }
}
