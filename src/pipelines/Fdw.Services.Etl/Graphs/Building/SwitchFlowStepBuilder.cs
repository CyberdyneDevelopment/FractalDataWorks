using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a switch (FDW-6134): a decision that fires the output that names the value of a variable.</summary>
[TypeOption(typeof(FlowStepBuilders), "Switch")]
public sealed class SwitchFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="SwitchFlowStepBuilder"/> class.</summary>
    public SwitchFlowStepBuilder() : base("Switch")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.VariableName is not { Length: > 0 } variable
            || DecisionTests.StatedText(component, SwitchComponentType.OutcomesSetting) is not { } outcomes
            || DecisionTests.StatedText(component, SwitchComponentType.OtherwiseSetting) is not { } otherwise)
        {
            return Missing(component, context);
        }

        return bool.TryParse(otherwise, out var hasOtherwise)
            ? GenericResult<IFlowStep>.Success(new FlowSwitchStep(component.Name, variable, SwitchComponentType.OutcomesOf(outcomes), hasOtherwise, context.Logger))
            : GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, $"otherwise '{otherwise}' is not true or false"));
    }
}
