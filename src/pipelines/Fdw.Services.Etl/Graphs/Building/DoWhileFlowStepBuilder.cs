using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a do-while loop: its steps run, then again while a condition over the variables holds.</summary>
[TypeOption(typeof(FlowStepBuilders), "DoWhile")]
public sealed class DoWhileFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="DoWhileFlowStepBuilder"/> class.</summary>
    public DoWhileFlowStepBuilder() : base("DoWhile")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.MaxIterations is not { } maximum)
        {
            return Missing(component, context);
        }

        // Why judged over the round (FDW-6134): a decision inside the body wrote what it fired in the scope of the round that just ran.
        if (DecisionTests.TestOutputOf(component) is { } testOutput)
        {
            return FlowFired.Parse(testOutput) is { } signal
                ? GenericResult<IFlowStep>.Success(new FlowDoWhile(component.Name, variables => FlowFired.Has(variables, signal), true, maximum, body, context.Logger))
                : GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, $"the test '{testOutput}' is not a decision output inside the loop"));
        }

        if (component.ConditionExpression is not { } expression)
        {
            return Missing(component, context);
        }

        var condition = FlowCondition.FromExpression(expression, context.Logger);
        if (condition.IsFailure || condition.Value is null)
        {
            return condition.ToNewResult<IFlowStep>();
        }

        return GenericResult<IFlowStep>.Success(new FlowDoWhile(component.Name, condition.Value, maximum, body, context.Logger));
    }
}
