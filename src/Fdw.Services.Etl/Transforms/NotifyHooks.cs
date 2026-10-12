using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Transforms;

/// <summary>
/// The Notify stages of a run, gathered from the whole stage tree, and the means to fire them at the run's lifecycle points
/// (FDW-1120).
/// </summary>
/// <remarks>
/// <para>
/// A Notify stage does not touch the records: in the fold it passes them through. It fires once per run at the run point it
/// states (<see cref="RunPoints"/>): after the final page has been through every stage, after the load has committed, or
/// when the run fails. The pipeline gathers the stages once when the run is prepared and fires them from the phase that
/// owns the point.
/// </para>
/// <para>
/// Why a run is checked before it reads anything: a rule deleted since the stage was saved, a host that cannot send, or a
/// message that names an unknown variable would otherwise surface only at the end, after the data was loaded.
/// </para>
/// </remarks>
internal sealed class NotifyHooks
{
    private readonly IReadOnlyList<NotifyStage> _hooks;

    private NotifyHooks(IReadOnlyList<NotifyStage> hooks)
    {
        _hooks = hooks;
    }

    /// <summary>Gets whether the run has no Notify stage.</summary>
    public bool IsEmpty => _hooks.Count == 0;

    /// <summary>Gathers the enabled Notify stages of a stage list, however deeply nested, in execution order.</summary>
    /// <param name="stages">The stages of the pipeline.</param>
    /// <returns>The hooks of the run.</returns>
    public static NotifyHooks Collect(IEnumerable<PipelineTransformConfiguration>? stages)
    {
        return new NotifyHooks(NotifyStages.Of(stages));
    }

    /// <summary>Confirms that every Notify stage of the run can send: the host has notification rules, the stage states what it needs and its rule exists.</summary>
    /// <param name="notifier">The host's rule notifier; null when the host has none.</param>
    /// <param name="pipelineName">The pipeline's name, for the refusal.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success, or the failure of the first stage that cannot send.</returns>
    public async Task<IGenericResult> Check(
        IRuleNotifier? notifier,
        string pipelineName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var hook in _hooks)
        {
            if (notifier is null)
            {
                return GenericResult.Failure(NotifyLog.NotifyRuleUnavailable(logger, pipelineName, hook.StageName));
            }

            var stated = Stated(hook);
            if (stated is not null)
            {
                return GenericResult.Failure(NotifyLog.NotifyStageInvalid(logger, hook.StageName, stated));
            }

            var unknown = NotifyTemplate.Unknown(hook.Message!);
            if (unknown.Count > 0)
            {
                return GenericResult.Failure(NotifyLog.NotifyTemplateUnknownVariable(
                    logger, hook.StageName, string.Join(", ", unknown), NotifyTemplate.Reserved));
            }

            var exists = await notifier.Exists(hook.RuleName!, cancellationToken).ConfigureAwait(false);
            if (exists.IsFailure)
            {
                return exists;
            }
        }

        return GenericResult.Success();
    }

    /// <summary>Fires every Notify stage whose run point the selector accepts, each once.</summary>
    /// <param name="firesAt">Chooses the run points to fire, for example <c>point => point.FiresAfterLoad</c>.</param>
    /// <param name="facts">What the run can say about itself now.</param>
    /// <param name="notifier">The host's rule notifier.</param>
    /// <param name="logger">The logger the outcomes are written to.</param>
    /// <param name="cancellationToken">Cancels the firing.</param>
    /// <returns>Success when every stage sent or its rule declined; otherwise the failure of the first stage that could not send. Every stage is tried.</returns>
    public async Task<IGenericResult> Fire(
        Func<IRunPoint, bool> firesAt,
        NotifyRunFacts facts,
        IRuleNotifier notifier,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        IGenericResult? failure = null;
        foreach (var hook in _hooks)
        {
            var attempted = await Attempt(hook, firesAt, facts, notifier, logger, cancellationToken).ConfigureAwait(false);
            if (attempted is { IsFailure: true } && failure is null)
            {
                failure = attempted;
            }
        }

        return failure is not null ? failure : GenericResult.Success();
    }

    // Null when the hook does not fire at the selected run points.
    private static async Task<IGenericResult?> Attempt(
        NotifyStage hook,
        Func<IRunPoint, bool> firesAt,
        NotifyRunFacts facts,
        IRuleNotifier notifier,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var stated = Stated(hook);
        if (stated is not null)
        {
            return GenericResult.Failure(NotifyLog.NotifyStageInvalid(logger, hook.StageName, stated));
        }

        return firesAt(RunPoints.ByName(hook.RunPoint!))
            ? await Send(hook, facts, notifier, logger, cancellationToken).ConfigureAwait(false)
            : null;
    }

    private static async Task<IGenericResult> Send(
        NotifyStage hook,
        NotifyRunFacts facts,
        IRuleNotifier notifier,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var message = NotifyTemplate.Expand(hook.StageName, hook.Message!, facts, logger);
        if (message.IsFailure)
        {
            return message;
        }

        var sent = await notifier.Notify(
            new RuleNotification
            {
                RuleName = hook.RuleName!,
                Subject = $"{facts.PipelineName}: {hook.StageName}",
                Message = message.Value!,
                PipelineId = facts.PipelineId,
                Metadata = Metadata(hook, facts),
                CorrelationId = facts.ExecutionId.ToString()
            },
            cancellationToken).ConfigureAwait(false);
        if (sent.IsFailure)
        {
            NotifyLog.NotifyFailed(logger, hook.StageName, hook.RunPoint!, hook.RuleName!, sent.CurrentMessage);
            return sent;
        }

        NotifyLog.NotifyFired(logger, hook.StageName, hook.RunPoint!, hook.RuleName!);
        return sent;
    }

    private static Dictionary<string, object?> Metadata(NotifyStage hook, NotifyRunFacts facts)
    {
        var metadata = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Stage"] = hook.StageName,
            ["RunPoint"] = hook.RunPoint
        };
        foreach (var variable in NotifyVariables.All())
        {
            metadata[variable.Name] = variable.ValueOf(facts);
        }

        return metadata;
    }

    private static string? Stated(NotifyStage hook)
    {
        if (string.IsNullOrWhiteSpace(hook.RuleName))
        {
            return "it states no notification rule";
        }

        if (string.IsNullOrWhiteSpace(hook.Message))
        {
            return "it states no message";
        }

        return string.IsNullOrWhiteSpace(hook.RunPoint) || ReferenceEquals(RunPoints.ByName(hook.RunPoint), RunPoints.NotFound)
            ? $"run point '{hook.RunPoint}' is not a run point"
            : null;
    }
}
