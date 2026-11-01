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
        if (component.ConditionExpression is not { } expression || component.MaxIterations is not { } maximum)
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
