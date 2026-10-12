using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Results;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Confirms, when a pipeline is saved or checked, that every Notify stage names a notification rule that exists (FDW-1120).
/// </summary>
/// <remarks>
/// What a Notify stage states of itself (a rule name, a run point, a message that uses only a run's variables) is judged by its
/// kind when the stage is mapped. Whether the rule exists is a fact about the host's configuration, so it is judged here, once
/// the stages are mapped, with the host's <see cref="IRuleNotifier"/>. A host that has none cannot send, so a Notify stage on it
/// is refused rather than saved to fail at its first run.
/// </remarks>
public static class PipelineNotifyCheck
{
    /// <summary>Checks the Notify stages of a mapped stage tree.</summary>
    /// <param name="transforms">The mapped stages of the pipeline.</param>
    /// <param name="notifier">The host's rule notifier; null when the host has none.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>Success, or a refusal that names the first stage whose rule cannot be used, with the path that addresses it.</returns>
    public static async Task<IGenericResult> Check(
        IEnumerable<PipelineTransformConfiguration> transforms,
        IRuleNotifier? notifier,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        foreach (var stage in NotifyStages.Of(transforms))
        {
            if (notifier is null)
            {
                return Refused(stage, "this host cannot send through notification rules", logger);
            }

            var exists = await notifier.Exists(stage.RuleName!, cancellationToken).ConfigureAwait(false);
            if (exists.IsFailure)
            {
                return Refused(stage, exists.CurrentMessage, logger);
            }
        }

        return GenericResult.Success();
    }

    // The same coded refusal the stage mapper gives a stage the caller got wrong, so it reaches the caller as a 400 problem on
    // the stage rather than a server fault.
    private static IGenericResult Refused(NotifyStage stage, string? reason, ILogger logger) =>
        GenericResult.Failure(
            EtlResultCodes.ByName("InvalidPipelineStep"),
            logger,
            ResultDetails.Create(
                "StepName", string.Join('.', stage.StagePath),
                "Reason", reason,
                PipelineStagePath.DetailKey, PipelineStagePath.Join(stage.StagePath)));
}
