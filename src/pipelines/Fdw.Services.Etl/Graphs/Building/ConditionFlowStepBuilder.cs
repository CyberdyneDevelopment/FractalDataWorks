using System.Collections.Generic;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a condition (FDW-6134): a decision that judges a condition over the variables and fires its true or its false output.</summary>
[TypeOption(typeof(FlowStepBuilders), "Condition")]
public sealed class ConditionFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionFlowStepBuilder"/> class.</summary>
    public ConditionFlowStepBuilder() : base("Condition")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.ConditionExpression is not { } expression)
        {
            return Missing(component, context);
        }

        var condition = FlowCondition.FromExpression(expression, context.Logger);
        return condition.IsFailure || condition.Value is null
            ? condition.ToNewResult<IFlowStep>()
            : GenericResult<IFlowStep>.Success(new FlowConditionStep(component.Name, condition.Value));
    }
}
