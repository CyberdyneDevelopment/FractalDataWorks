using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Services.Etl.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What looking up the transformer chains of a pipeline request's field mappings found: the id of every Transformation a
/// chain names, and every problem with a chain.
/// </summary>
/// <param name="Ids">The id of each Transformation the request names, by its name (case-insensitive).</param>
/// <param name="Problems">The problems found, each placed on its stage; empty when every chain can be saved.</param>
public sealed record StageChains(IReadOnlyDictionary<string, Guid> Ids, IReadOnlyList<PipelineProblem> Problems)
{
    /// <summary>
    /// Gets the lookup of a request that states no chain: nothing is named, so nothing is resolved and nothing is wrong.
    /// </summary>
    public static StageChains None { get; } = new(new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase), []);

    /// <summary>
    /// Gets the refusal that saving a request with these problems answers with: the coded invalid-step failure naming the
    /// first problem's stage and reason, or <see langword="null"/> when no chain has a problem.
    /// </summary>
    /// <typeparam name="T">The value type of the result the caller returns.</typeparam>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The failure, or null when there is nothing to refuse.</returns>
    public IGenericResult<T>? Refusal<T>(ILogger logger)
    {
        if (Problems.Count == 0)
        {
            return null;
        }

        var first = Problems[0];
        return GenericResult<T>.Failure(
            EtlResultCodes.ByName("InvalidPipelineStep"),
            logger,
            first.StagePath is null
                ? ResultDetails.Create("StepName", first.Stage, "Reason", first.Reason)
                : ResultDetails.Create("StepName", first.Stage, "Reason", first.Reason, PipelineStagePath.DetailKey, PipelineStagePath.Join([.. first.StagePath])));
    }
}
