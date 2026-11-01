using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Scheduling.Clients.Abstractions;

/// <summary>
/// Resolves the pipeline a schedule names, by id, in the view of whoever is saving the schedule (FDW-1088).
/// </summary>
/// <remarks>
/// The check belongs where the caller's identity is: the API proxy, whose pipeline client carries the caller's
/// own token to the pipeline API, so a schedule can name only a pipeline its author can see. The scheduler has no
/// tenant of its own and stores what the proxy resolved without looking again.
/// </remarks>
public static class SchedulePipeline
{
    /// <summary>Resolves <paramref name="pipelineId"/> as the caller sees it.</summary>
    /// <param name="pipelines">The pipeline client, carrying the caller's identity.</param>
    /// <param name="scheduleName">The schedule being saved, for the refusal.</param>
    /// <param name="pipelineId">The pipeline the schedule names.</param>
    /// <param name="logger">The endpoint's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The pipeline, a refusal when the caller cannot see it, or the failure that stopped the lookup.</returns>
    public static async Task<IGenericResult<PipelineDetailResponse>> Resolve(
        IPipelineClient pipelines,
        string scheduleName,
        Guid pipelineId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (pipelines is null) throw new ArgumentNullException(nameof(pipelines));

        var pipeline = await pipelines.Get(pipelineId, cancellationToken).ConfigureAwait(false);
        if (pipeline.IsFailure)
        {
            // Why the category: the pipeline API answers a pipeline the caller cannot see with a Missing-category
            // code. Any other failure means the check could not be made; it is passed on as it is, never reported
            // as a missing pipeline.
            return pipeline.Code is { } code && ReferenceEquals(ResultCategories.ById(code.Id / 10000), ResultCategories.Missing)
                ? GenericResult<PipelineDetailResponse>.Failure(SchedulePipelineLog.SchedulePipelineNotFound(logger, scheduleName, pipelineId))
                : pipeline;
        }

        return pipeline.Value is null
            ? GenericResult<PipelineDetailResponse>.Failure(SchedulePipelineLog.SchedulePipelineLookupEmpty(logger, scheduleName, pipelineId))
            : pipeline;
    }
}
