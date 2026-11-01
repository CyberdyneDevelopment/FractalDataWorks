using System;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Transforms;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Builds a stage-backed tool (Filter, Sort and the rest) into the component that runs its stage kind on every row that passes through.
/// </summary>
/// <remarks>
/// One builder shape serves every such tool: the tool is its stage kind, so the component holds exactly one stage of that kind and
/// the adapter runs it through the one transform code the stage list always used.
/// </remarks>
public abstract class StageToolFlowComponentBuilder : FlowComponentBuilderBase
{
    private readonly string _stageKind;

    /// <summary>Initializes a new instance of the <see cref="StageToolFlowComponentBuilder"/> class.</summary>
    /// <param name="name">The tool's name, which is the stage kind it runs.</param>
    protected StageToolFlowComponentBuilder(string name) : this(name, name)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="StageToolFlowComponentBuilder"/> class for a tool whose name is not its stage kind's.</summary>
    /// <param name="name">The tool's name.</param>
    /// <param name="stageKind">The stage kind the tool runs.</param>
    protected StageToolFlowComponentBuilder(string name, string stageKind) : base(name)
    {
        _stageKind = stageKind;
    }

    /// <summary>
    /// Says why a stage of this kind cannot run as a tool in a graph, when it states something the graph has no place for.
    /// </summary>
    /// <param name="component">The stored component.</param>
    /// <param name="stage">The stage it holds.</param>
    /// <param name="context">What the run builds with.</param>
    /// <returns>The refusal, or <see langword="null"/> when the stage can run.</returns>
    protected virtual IGenericResult<IFlowComponent>? Unsupported(PipelineComponentConfiguration component, PipelineTransformConfiguration stage, FlowBuildContext context) => null;

    /// <inheritdoc />
    public override IGenericResult<IFlowComponent> Create(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.Stages.Count != 1)
        {
            return MissingSettings.Of(component, context);
        }

        var stage = component.Stages[0];
        if (!string.Equals(stage.OperationType, _stageKind, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(
                context.Logger, component.Name, component.ComponentType, $"the tool runs the stage kind '{_stageKind}', but the stage it holds is of kind '{stage.OperationType}'"));
        }

        if (TransformTypes.ByName(stage.OperationType) is not TransformTypeBase kind || ReferenceEquals(kind, TransformTypes.NotFound))
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(
                context.Logger, component.Name, component.ComponentType, $"the stage kind '{stage.OperationType}' is not one this platform has"));
        }

        if (kind.RequiresCompleteInput && component.BatchSize is null)
        {
            return MissingSettings.Of(component, context);
        }

        var unsupported = Unsupported(component, stage, context);
        if (unsupported is not null)
        {
            return unsupported;
        }

        if (component.ErrorOutput is null && kind.RejectsRecords(stage))
        {
            return GenericResult<IFlowComponent>.Failure(GraphLog.ComponentInvalid(
                context.Logger, component.Name, component.ComponentType, "the tool rejects records, so the component names an error output to take them (errorOutput)"));
        }

        return GenericResult<IFlowComponent>.Success(
            new FlowStageAdapter(component.Name, kind, stage, context.NewTransformContext(), component.BatchSize, component.ErrorOutput));
    }
}
