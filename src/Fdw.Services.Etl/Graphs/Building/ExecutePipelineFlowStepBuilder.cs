using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a step that starts another pipeline and optionally waits for it to end.</summary>
[TypeOption(typeof(FlowStepBuilders), "ExecutePipeline")]
public sealed class ExecutePipelineFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="ExecutePipelineFlowStepBuilder"/> class.</summary>
    public ExecutePipelineFlowStepBuilder() : base("ExecutePipeline")
    {
    }

    /// <inheritdoc />
    public override bool HasOutsideEffect => true;

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.TargetPipeline is not { } pipeline || component.AwaitCompletion is not { } waits)
        {
            return Missing(component, context);
        }

        List<FlowStepParameter> parameters = [];
        foreach (var parameter in component.Parameters)
        {
            var text = parameter.ValueText;
            parameters.Add(new FlowStepParameter(parameter.ParameterName, variables => VariableText.Render(text, variables)));
        }

        // Why the run point is judged here too: a name that is not a run point is refused when the pipeline is built, not when the run is failing.
        IRunPoint? point = null;
        if (component.RunPoint is { } runPoint)
        {
            point = RunPoints.ByName(runPoint);
            if (ReferenceEquals(point, RunPoints.NotFound))
            {
                return GenericResult<IFlowStep>.Failure(
                    GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, $"the run point '{runPoint}' is not a run point"));
            }
        }

        // A step that does not wait has nothing to fail on; one that waits states whether it fails, and the graph check has
        // already refused a waiting step that does not.
        return GenericResult<IFlowStep>.Success(new FlowExecutePipeline(
            component.Name,
            pipeline,
            parameters,
            waits,
            component.ResultVariable,
            component.FailOnChildFailure is true,
            context.Logger,
            point,
            point is null ? null : new FlowNotifyRun(context.PipelineId, context.PipelineName, context.ExecutionId, context.StartedAt),
            point is null ? null : () => context.LoadedRows()));
    }
}
