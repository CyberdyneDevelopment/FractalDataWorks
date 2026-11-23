using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Logging;

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
        if (component.AwaitCompletion is not { } waits)
        {
            return Missing(component, context);
        }

        var target = TargetOf(component, context);
        if (target.IsFailure || target.Value is not { } resolvedTarget)
        {
            return target.ToNewResult<IFlowStep>();
        }

        var outputs = OutputsOf(component, waits, context);
        if (outputs.IsFailure || outputs.Value is not { } mapped)
        {
            return outputs.ToNewResult<IFlowStep>();
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
            resolvedTarget,
            parameters,
            waits,
            component.ResultVariable,
            component.FailOnChildFailure is true,
            context.Logger,
            point,
            point is null ? null : new FlowNotifyRun(context.PipelineId, context.PipelineName, context.ExecutionId, context.StartedAt),
            point is null ? null : () => context.LoadedRows(),
            mapped));
    }

    // Why one of the two and never both or neither (FDW-6135): the pipeline is the author's, or a variable's from a list the author states; the save refuses a component that states it otherwise.
    private static IGenericResult<FlowPipelineTarget> TargetOf(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        var variable = DecisionTests.StatedText(component, ExecutePipelineComponentType.TargetVariableSetting);
        if (component.TargetPipeline is { Length: > 0 } pipeline && variable is null)
        {
            return GenericResult<FlowPipelineTarget>.Success(FlowPipelineTarget.Named(pipeline));
        }

        if (variable is not null && component.TargetPipeline is null && DecisionTests.StatedText(component, ExecutePipelineComponentType.TargetsSetting) is { } listed
            && ExecutePipelineComponentType.TargetsOf(listed) is { Count: > 0 } targets)
        {
            return GenericResult<FlowPipelineTarget>.Success(FlowPipelineTarget.FromVariable(variable, targets));
        }

        return GenericResult<FlowPipelineTarget>.Failure(GraphLog.StepInvalid(
            context.Logger, component.Name, component.ComponentType, "it states the pipeline to start once, as a pipeline or as a targetVariable with the targets it may start"));
    }

    // The outputs of the started pipeline the step reads into variables (FDW-6136): lines of an output name and a variable, and only for a step that waits.
    private static IGenericResult<IReadOnlyList<KeyValuePair<string, string>>> OutputsOf(PipelineComponentConfiguration component, bool waits, FlowBuildContext context)
    {
        if (DecisionTests.StatedText(component, ExecutePipelineComponentType.OutputsSetting) is not { } mapped)
        {
            return GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Success([]);
        }

        if (!ExecutePipelineComponentType.TryReadOutputs(mapped, out var outputs))
        {
            return GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Failure(PipelineOutputLog.OutputMapMalformed(context.Logger, component.Name, mapped));
        }

        return !waits && outputs.Count > 0
            ? GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Failure(PipelineOutputLog.OutputsNeedAwait(context.Logger, component.Name))
            : GenericResult<IReadOnlyList<KeyValuePair<string, string>>>.Success(outputs);
    }
}
