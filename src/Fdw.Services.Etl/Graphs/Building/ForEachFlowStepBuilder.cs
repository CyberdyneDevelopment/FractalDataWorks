using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a for-each loop: its steps run once for each element of a list held in a variable.</summary>
[TypeOption(typeof(FlowStepBuilders), "ForEach")]
public sealed class ForEachFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ForEachFlowStepBuilder"/> class.</summary>
    public ForEachFlowStepBuilder() : base("ForEach")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.VariableName is not { } variable || component.ListVariable is not { } list)
        {
            return Missing(component, context);
        }

        return component.BatchSize < 1
            ? GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, $"takes {component.BatchSize} items a round, which is not at least one"))
            : GenericResult<IFlowStep>.Success(new FlowForEach(component.Name, variable, list, body, component.BatchSize, context.Logger));
    }
}
