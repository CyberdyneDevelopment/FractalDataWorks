using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Judges an edit of a saved pipeline without saving it (FDW-1105): every check <see cref="UpdatePipelineEndpointBase{TConfig}"/>
/// makes before it writes, listed instead of stopping at the first refusal.
/// </summary>
/// <remarks>
/// Why this agrees with the update: it calls the very pieces the update calls -- the transform mapper, the load draft judging
/// what <see cref="PipelineLoadPatch"/> says the saved pipeline would carry and the truncate consistency rule -- on the same inputs, in the same order. It never calls the pieces that write: no save.
/// </remarks>
public sealed class SavedPipelineCheck
{
    private readonly PipelineLoadDraft _loadDraft;
    private readonly StageChainLookup _chains;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="SavedPipelineCheck"/> class.</summary>
    /// <param name="loadDraft">Judges the load definition the pipeline would carry.</param>
    /// <param name="chains">Looks up the Transformations the field mappings' chains apply.</param>
    /// <param name="logger">The logger the refusals are written to.</param>
    public SavedPipelineCheck(PipelineLoadDraft loadDraft, StageChainLookup chains, ILogger logger)
    {
        _loadDraft = loadDraft;
        _chains = chains;
        _logger = logger;
    }

    /// <summary>Lists every problem the update would be refused for.</summary>
    /// <param name="request">The update body.</param>
    /// <param name="original">The saved pipeline the body would be applied to.</param>
    /// <param name="notifier">The host's rule notifier, which confirms a Notify stage's rule exists; null when the host has none.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <returns>The problems in the order the checks ran; empty when the update would not be refused for any reason these checks can see.</returns>
    public async Task<IReadOnlyList<PipelineProblem>> Problems(
        UpdatePipelineRequest request,
        IPipelineImplementationConfiguration original,
        IRuleNotifier? notifier,
        CancellationToken cancellationToken = default)
    {
        var problems = new List<PipelineProblem>();
        var existing = PipelineDetailMapper.Map(original);
        if (request.Transforms is not null)
        {
            var chains = await _chains.Resolve(request.Name, request.Transforms, cancellationToken).ConfigureAwait(false);
            problems.AddRange(chains.Problems);
            if (chains.Problems.Count == 0)
            {
                var transforms = PipelineTransformConfigurationMapper.Map(existing.PipelineType, request.Transforms, _logger, chains.Ids);
                if (transforms.IsFailure)
                {
                    problems.Add(PipelineProblems.OfStageRefusal(transforms, "mapping the stages"));
                }
                else if (await PipelineNotifyCheck.Check(transforms.Value!, notifier, _logger, cancellationToken).ConfigureAwait(false) is { IsFailure: true } refused)
                {
                    problems.Add(PipelineProblems.OfStageRefusal(refused, "checking the notification rules"));
                }
            }
        }

        var effective = PipelineLoadPatch.Effective(request, original);
        if (PipelineLoadPatch.Changes(request))
        {
            problems.AddRange(await _loadDraft.Problems(
                effective,
                PipelineLoadPatch.EffectiveDestinationDataSet(request, existing),
                PipelineLoadPatch.Expressions(request, original),
                cancellationToken).ConfigureAwait(false));
        }

        if (PipelineTruncateConsistency.Check(request.TruncateBeforeLoad, effective.LoadMode, _logger) is { } disagreement)
        {
            problems.Add(PipelineProblems.OfPipeline("TruncateBeforeLoad", disagreement.Message));
        }

        return problems;
    }
}
