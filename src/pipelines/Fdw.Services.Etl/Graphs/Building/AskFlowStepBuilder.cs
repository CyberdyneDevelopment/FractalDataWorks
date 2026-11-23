using System;
using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Logging;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// Builds an Ask step (FDW-6142): a decision that judges a call under an answer policy and, when the call needs the person, keeps an answer request for them and tells them.
/// </summary>
/// <remarks>
/// It states no outside effect to the base: the step is a decision and keeps its own claim (the request is unique for the step of the run's chain), so the claim wrapper that hides a decision's outputs is not
/// applied. A test run builds a step that judges nothing and keeps nothing. It reads what it needs from the build context (<see cref="FlowBuildContext.AskServices"/>, as Execute pipeline reads the starter and Send the notifier), as
/// FDW's own kinds do; a host that registers none refuses to build the step.
/// </remarks>
[TypeOption(typeof(FlowStepBuilders), "Ask")]
public sealed class AskFlowStepBuilder : FlowStepBuilderBase
{
    /// <summary>Initializes a new instance of the <see cref="AskFlowStepBuilder"/> class.</summary>
    public AskFlowStepBuilder() : base("Ask")
    {
    }

    /// <inheritdoc />
    public override IGenericResult<IFlowStep> Create(PipelineComponentConfiguration component, IReadOnlyList<IFlowStep> body, FlowBuildContext context)
    {
        if (context.Test is not null)
            return GenericResult<IFlowStep>.Success(new FlowAskTestStep(component.Name, context.Logger));

        var settings = SettingsOf(component, context);
        if (settings.IsFailure || settings.Value is not { } stated)
            return settings.ToNewResult<IFlowStep>();

        if (context.AskServices is not { } services)
            return GenericResult<IFlowStep>.Failure(GraphLog.RunDependencyMissing(context.Logger, context.PipelineName, "the answer requests"));

        return context.Notifier is { } notifier
            ? GenericResult<IFlowStep>.Success(new FlowAskStep(
                component.Name, stated, services, notifier, new FlowNotifyRun(context.PipelineId, context.PipelineName, context.ExecutionId, context.StartedAt), context.Logger))
            : GenericResult<IFlowStep>.Failure(GraphLog.RunDependencyMissing(context.Logger, context.PipelineName, "the notification service"));
    }

    private static IGenericResult<AskStepSettings> SettingsOf(PipelineComponentConfiguration component, FlowBuildContext context)
    {
        if (component.NotificationRuleName is not { } rule
            || DecisionTests.StatedText(component, AskComponentType.PolicySetting) is not { } policy
            || DecisionTests.StatedText(component, AskComponentType.CallSetting) is not { } call
            || DecisionTests.StatedText(component, AskComponentType.KindSetting) is not { } kind
            || DecisionTests.StatedText(component, AskComponentType.ContinuationSetting) is not { } continuation
            || DecisionTests.StatedText(component, AskComponentType.AnswerParameterSetting) is not { } answerParameter
            || DecisionTests.StatedText(component, AskComponentType.TitleSetting) is not { } title
            || DecisionTests.StatedText(component, AskComponentType.BodySetting) is not { } text)
        {
            return GenericResult<AskStepSettings>.Failure(GraphLog.StepInvalid(context.Logger, component.Name, component.ComponentType, "a setting its kind requires is not stated"));
        }

        var found = AnswerRequestKinds.ByName(kind);
        if (ReferenceEquals(found, AnswerRequestKinds.NotFound))
            return GenericResult<AskStepSettings>.Failure(AnswerRequestLog.KindUnknown(context.Logger, component.Name, kind));

        var options = OptionsOf(component, found, context);
        if (options.IsFailure || options.Value is not { } offered)
            return options.ToNewResult<AskStepSettings>();

        var arguments = ArgumentsOf(component, answerParameter, context);
        if (arguments.IsFailure || arguments.Value is not { } given)
            return arguments.ToNewResult<AskStepSettings>();

        return GenericResult<AskStepSettings>.Success(new AskStepSettings(
            policy, call, found.Name, continuation, answerParameter, rule, title, text, offered, DecisionTests.StatedText(component, AskComponentType.GrantVariableSetting), given));
    }

    // A choice states its options and a question states none.
    private static IGenericResult<IReadOnlyList<string>> OptionsOf(PipelineComponentConfiguration component, IAnswerRequestKind kind, FlowBuildContext context)
    {
        IReadOnlyList<string> options = DecisionTests.StatedText(component, AskComponentType.OptionsSetting) is { } listed ? AskComponentType.OptionsOf(listed) : [];
        if (kind.AnswerMustBeAnOption && options.Count == 0)
            return GenericResult<IReadOnlyList<string>>.Failure(AnswerRequestLog.ChoiceNeedsOptions(context.Logger, component.Name));

        return !kind.AnswerMustBeAnOption && options.Count > 0
            ? GenericResult<IReadOnlyList<string>>.Failure(AnswerRequestLog.QuestionTakesNoOptions(context.Logger, component.Name))
            : GenericResult<IReadOnlyList<string>>.Success(options);
    }

    // The arguments the continuation is started with, none of them the parameter the answer goes to.
    private static IGenericResult<IReadOnlyList<FlowStepParameter>> ArgumentsOf(PipelineComponentConfiguration component, string answerParameter, FlowBuildContext context)
    {
        List<FlowStepParameter> arguments = [];
        foreach (var parameter in component.Parameters)
        {
            if (string.Equals(parameter.ParameterName, answerParameter, StringComparison.OrdinalIgnoreCase))
                return GenericResult<IReadOnlyList<FlowStepParameter>>.Failure(AnswerRequestLog.ArgumentNamedLikeTheAnswer(context.Logger, component.Name, parameter.ParameterName));

            var value = parameter.ValueText;
            arguments.Add(new FlowStepParameter(parameter.ParameterName, variables => VariableText.Render(value, variables)));
        }

        return GenericResult<IReadOnlyList<FlowStepParameter>>.Success(arguments);
    }
}
