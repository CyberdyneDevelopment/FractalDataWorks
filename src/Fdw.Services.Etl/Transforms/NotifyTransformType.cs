using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Microsoft.Extensions.Logging;
using OptionTransformTypes = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// Transform type that sends a notification through a named notification rule at a point in the run (FDW-1120).
/// </summary>
/// <remarks>
/// <para>
/// A Notify stage states a notification rule, a run point (<see cref="RunPoints"/>) and a message. It never touches the
/// records: in the fold it passes them through. The run fires it once, at its run point, through <see cref="NotifyHooks"/>;
/// the rule chooses the channel and severity and its own conditions (enabled, scope) still decide whether it sends.
/// </para>
/// <para>
/// Why no default: the stage states the point it fires at and what it says. A rule that does not exist, a run point that
/// is not one, or a message that names a variable no run has is refused when the pipeline is saved and again when the run
/// starts, never sent as written or skipped.
/// </para>
/// </remarks>
[TypeOption(typeof(OptionTransformTypes), "Notify")]
public sealed class NotifyTransformType : TransformTypeBase
{
    private const string Kind = "Notify";

    private static readonly StageMember[] OwnedMembers =
    [
        new("notification", "Notification", true,
            "The notification rule to send through, the run point it fires at (AfterStage, AfterLoad or OnFailure) and the message; the message may use {{PipelineName}}, {{ExecutionId}}, {{RowsExtracted}}, {{RowsRejected}} and {{RowsLoaded}}")
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyTransformType"/> class.
    /// </summary>
    public NotifyTransformType() : base(
        name: Kind,
        displayName: "Run Notification",
        description: "Sends a notification through a notification rule once per run, after the final page, after the load or when the run fails",
        category: "Notification",
        modifiesStructure: false,
        canFilterRecords: false,
        inputPorts: ["input"],
        outputPorts: ["output"])
    {
    }

    /// <inheritdoc />
    public override IReadOnlyList<StageMember> Members => OwnedMembers;

    /// <inheritdoc />
    public override Task<IGenericResult<IDictionary<string, object?>>> Transform(
        IDictionary<string, object?> input,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(configuration is PipelineTransformConfiguration
            ? GenericResult<IDictionary<string, object?>>.Success(input)
            : GenericResult<IDictionary<string, object?>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name)));

    /// <inheritdoc />
    public override Task<IGenericResult<IEnumerable<IDictionary<string, object?>>>> TransformBatch(
        IEnumerable<IDictionary<string, object?>> inputs,
        IGenericConfiguration configuration,
        ITransformContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(configuration is PipelineTransformConfiguration
            ? GenericResult<IEnumerable<IDictionary<string, object?>>>.Success(inputs)
            : GenericResult<IEnumerable<IDictionary<string, object?>>>.Failure(
                EtlLog.WrongConfigurationType(context.Logger, Kind, configuration.GetType().Name)));

    /// <inheritdoc />
    public override IGenericResult MapSpecToConfiguration(ITransformOperationSpec spec, IGenericConfiguration target, ILogger logger)
    {
        if (target is not PipelineTransformConfiguration config)
        {
            return GenericResult.Failure(EtlLog.WrongConfigurationType(logger, spec.Name, target.GetType().Name));
        }

        if (spec is not ITransformNotifyingSpec { Notification: { } notification })
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, "the request does not state a notification (ruleName, runPoint, message)"));
        }

        var problem = FirstProblem(notification);
        if (problem is not null)
        {
            return GenericResult.Failure(TransformLog.TransformParamsInvalid(logger, spec.Name, Kind, problem));
        }

        config.RunPoint = RunPoints.ByName(notification.RunPoint).Name;
        config.NotificationRuleName = notification.RuleName;
        config.NotificationMessage = notification.Message;

        EtlLog.TransformSpecMapped(logger, spec.Name, spec.OperationType);
        return GenericResult.Success();
    }

    private static string? FirstProblem(INotificationSpec notification)
    {
        if (string.IsNullOrWhiteSpace(notification.RuleName))
        {
            return "a notify names the notification rule it sends through (ruleName)";
        }

        if (string.IsNullOrWhiteSpace(notification.RunPoint))
        {
            return "a notify states the run point it fires at (runPoint)";
        }

        if (ReferenceEquals(RunPoints.ByName(notification.RunPoint), RunPoints.NotFound))
        {
            return $"runPoint '{notification.RunPoint}' is not a run point";
        }

        if (string.IsNullOrWhiteSpace(notification.Message))
        {
            return "a notify states its message";
        }

        var unknown = NotifyTemplate.Unknown(notification.Message);
        return unknown.Count == 0
            ? null
            : $"message uses unknown variables [{string.Join(", ", unknown)}]; a run has [{NotifyTemplate.Reserved}]";
    }
}
