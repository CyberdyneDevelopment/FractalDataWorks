using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a step that sets a declared variable to a value of a stated type, or to the result of a calculation (FDW-2042).</summary>
[TypeOption(typeof(FlowStepBuilders), "SetVariable")]
public sealed class SetVariableFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="SetVariableFlowStepBuilder"/> class.</summary>
    public SetVariableFlowStepBuilder() : base("SetVariable")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.TransformationName is not null)
        {
            return Calculated(component, context);
        }

        if (component.VariableName is not { } variable || component.ConstantType is not { } typeName)
        {
            return Missing(component, context);
        }

        if (!GraphValues.TryRead(typeName, component.ConstantText, out var typed))
        {
            return GenericResult<IFlowStep>.Failure(
                GraphLog.StepSettingUnreadable(context.Logger, component.Name, "value", component.ConstantText, typeName));
        }

        return GenericResult<IFlowStep>.Success(new FlowSetVariable(component.Name, variable, _ => typed, context.Logger));
    }

    // The result of one named field transformation applied to a value of the run (FDW-1957): the value is required, and its type is the
    // transformation's, so a stated type has no meaning here and the design check refuses it.
    private static IGenericResult<IFlowStep> Calculated(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.VariableName is not { } variable || component.ConstantText is not { } input || component.TransformationName is not { } transformation)
        {
            return Missing(component, context);
        }

        List<FlowStepParameter> parameters = [];
        foreach (var parameter in component.Parameters)
        {
            var text = parameter.ValueText;
            parameters.Add(new FlowStepParameter(parameter.ParameterName, variables => VariableText.Render(text, variables)));
        }

        return GenericResult<IFlowStep>.Success(new FlowSetVariable(
            component.Name, variable, new FlowVariableCalculation(VariableText.Typed(input), transformation, parameters, context.Logger), context.Logger));
    }
}
