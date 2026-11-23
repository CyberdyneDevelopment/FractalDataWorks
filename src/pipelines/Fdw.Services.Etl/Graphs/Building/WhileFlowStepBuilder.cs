using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a while loop (FDW-6134): its test is judged before its body, so the body may run no time.</summary>
[TypeOption(typeof(FlowStepBuilders), "While")]
public sealed class WhileFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="WhileFlowStepBuilder"/> class.</summary>
    public WhileFlowStepBuilder() : base("While")
    {
    }

    /// <inheritdoc />
    /// <remarks>The graph builder hands a loop whose test is a decision's output the decisions as a last step of its body, a <see cref="FlowLoopTest"/>.</remarks>
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.MaxIterations is not { } maximum)
        {
            return Missing(component, context);
        }

        var decisions = body.OfType<FlowLoopTest>().FirstOrDefault();
        List<IFlowStep> steps = [.. body.Where(step => step is not FlowLoopTest)];
        if (DecisionTests.TestOutputOf(component) is { } testOutput)
        {
            return FlowFired.Parse(testOutput) is { } signal && decisions is not null
                ? GenericResult<IFlowStep>.Success(new FlowWhile(component.Name, variables => FlowFired.Has(variables, signal), decisions, maximum, steps, context.Logger))
                : GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, $"the test '{testOutput}' is not a decision output inside the loop"));
        }

        if (component.ConditionExpression is not { } expression)
        {
            return Missing(component, context);
        }

        var condition = FlowCondition.FromExpression(expression, context.Logger);
        return condition.IsFailure || condition.Value is null
            ? condition.ToNewResult<IFlowStep>()
            : GenericResult<IFlowStep>.Success(new FlowWhile(component.Name, condition.Value, null, maximum, steps, context.Logger));
    }
}
