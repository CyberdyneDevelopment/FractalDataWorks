using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The checks on a pipeline draft that creating a pipeline and validating a draft must answer the same way, held
/// in one place so the two cannot drift (FDW-1095).
/// </summary>
/// <remarks>
/// Why this is shared and not copied: a dry run that disagreed with create would tell the Pipeline Builder a draft
/// is fine and then fail on save. The transform mapping (<see cref="PipelineTransformConfigurationMapper"/>) and the
/// run-role authority (<see cref="PipelineRunAuthority"/>) are already single code paths both endpoints call; the
/// name check lives here for the same reason.
/// </remarks>
public static class PipelineDraft
{
    /// <summary>Gets whether a pipeline with <paramref name="name"/> already exists.</summary>
    /// <param name="provider">The pipeline configuration provider.</param>
    /// <param name="name">The name the draft would take.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the name is taken.</returns>
    public static async Task<bool> NameTaken(
        PipelineServiceConfigurationProvider provider, string name, CancellationToken cancellationToken = default)
    {
        var existing = await provider.Get(name, cancellationToken).ConfigureAwait(false);
        return existing.IsSuccess && existing.Value != null;
    }

    /// <summary>The filter expression of each transform a body states, with the stage it sits on, for the run-parameter reference check.</summary>
    /// <param name="transforms">The transforms of the body.</param>
    /// <returns>Each transform's name and filter expression.</returns>
    public static IReadOnlyList<(string? Stage, string? Expression)> StageFilterExpressions(IEnumerable<CreatePipelineTransformRequest> transforms) =>
        [.. transforms.Select(t => ((string?)t.Name, t.FilterExpression))];
}
