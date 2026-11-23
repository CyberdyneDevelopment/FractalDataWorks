using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Notifications.Abstractions;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>Builds a Send step (FDW-6138): a notification through a rule, its words and its payload made from the variables.</summary>
[TypeOption(typeof(FlowStepBuilders), "Send")]
public sealed class SendFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="SendFlowStepBuilder"/> class.</summary>
    public SendFlowStepBuilder() : base("Send")
    {
    }

    /// <inheritdoc />
    public override bool HasOutsideEffect => true;

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (component.NotificationRuleName is not { } rule || component.NotificationMessage is not { } message
            || DecisionTests.StatedText(component, SendComponentType.SubjectSetting) is not { } subject)
        {
            return Missing(component, context);
        }

        if (context.Notifier is not { } notifier)
        {
            return GenericResult<IFlowStep>.Failure(GraphLog.RunDependencyMissing(context.Logger, context.PipelineName, "the notification service"));
        }

        var payloadType = DecisionTests.StatedText(component, SendComponentType.PayloadTypeSetting);
        if (payloadType is not null && ReferenceEquals(NotificationPayloadTypes.ByName(payloadType), NotificationPayloadTypes.NotFound))
        {
            return GenericResult<IFlowStep>.Failure(FlowLog.NotificationPayloadTypeUnknown(context.Logger, component.Name, payloadType));
        }

        List<FlowStepParameter> fields = [];
        foreach (var parameter in component.Parameters)
        {
            var text = parameter.ValueText;
            fields.Add(new FlowStepParameter(parameter.ParameterName, variables => VariableText.Render(text, variables)));
        }

        return GenericResult<IFlowStep>.Success(new FlowSend(
            component.Name, notifier, rule, subject, message, payloadType, fields, new FlowNotifyRun(context.PipelineId, context.PipelineName, context.ExecutionId, context.StartedAt), context.Logger));
    }
}
