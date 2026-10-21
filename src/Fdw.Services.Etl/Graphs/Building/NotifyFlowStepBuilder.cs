using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds the Notify step: a notification through a rule, sent when the run reaches the step or only if the run fails.</summary>
[TypeOption(typeof(FlowStepBuilders), "Notify")]
public sealed class NotifyFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="NotifyFlowStepBuilder"/> class.</summary>
    public NotifyFlowStepBuilder() : base("Notify")
    {
    }

    /// <inheritdoc />
    public override bool HasOutsideEffect => true;

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.NotificationRuleName is not { } rule || component.RunPoint is not { } runPoint || component.NotificationMessage is not { } message)
        {
            return Missing(component, context);
        }

        var point = RunPoints.ByName(runPoint);
        if (ReferenceEquals(point, RunPoints.NotFound))
        {
            return GenericResult<IFlowStep>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, $"the run point '{runPoint}' is not a run point"));
        }

        if (context.Notifier is not { } notifier)
        {
            return GenericResult<IFlowStep>.Failure(GraphLog.RunDependencyMissing(context.Logger, context.PipelineName, "the notification service"));
        }

        return GenericResult<IFlowStep>.Success(new FlowNotify(
            component.Name,
            notifier,
            rule,
            point,
            message,
            new FlowNotifyRun(context.PipelineId, context.PipelineName, context.ExecutionId, context.StartedAt),
            () => context.LoadedRows(),
            context.Logger));
    }
}
