using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a gate (FDW-6134): a decision that combines the lines that feed it by its logic.</summary>
[TypeOption(typeof(FlowStepBuilders), "Gate")]
public sealed class GateFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="GateFlowStepBuilder"/> class.</summary>
    public GateFlowStepBuilder() : base("Gate")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (DecisionTests.StatedText(component, GateComponentType.LogicSetting) is not { } text)
        {
            return Missing(component, context);
        }

        var logic = GateLogics.ByName(text);
        return ReferenceEquals(logic, GateLogics.NotFound)
            ? GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, $"the logic '{text}' is not one of GateLogics"))
            : GenericResult<IFlowStep>.Success(new FlowGateStep(component.Name, logic, context.Logger));
    }
}
