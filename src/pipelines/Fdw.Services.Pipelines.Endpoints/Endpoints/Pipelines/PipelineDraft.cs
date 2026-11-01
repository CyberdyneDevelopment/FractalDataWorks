using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The check on a graph draft that creating and validating must answer the same way: whether the name is taken (FDW-1095).
/// </summary>
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
}
