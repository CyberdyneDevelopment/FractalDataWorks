using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Reads the organization that owns a pipeline (FDW-4276): the one answer every live broadcast of a run is addressed by.
/// </summary>
/// <remarks>
/// The firehose group a run's events go to is <c>org:{orgId}:pipeline-updates</c>, so a run is addressed by its pipeline's owning organization and never by the
/// tenant. A pipeline that states no organization has none (null) and is broadcast to no firehose, as the hub documents; a read that failed is a failure the caller
/// logs, not the same answer as "no organization".
/// </remarks>
internal static class OwningOrg
{
    /// <summary>Reads the owning organization of a pipeline.</summary>
    /// <param name="services">The scope's services, which supply the pipeline configuration provider.</param>
    /// <param name="pipelineId">The pipeline's Id.</param>
    /// <param name="cancellationToken">A token that ends the read.</param>
    /// <returns>The organization's Id, null when the pipeline states none, or the read's own failure.</returns>
    internal static async Task<IGenericResult<Guid?>> Of(IServiceProvider services, Guid pipelineId, CancellationToken cancellationToken)
    {
        var read = await services.GetRequiredService<IPipelineConfigurationProvider>().Get(pipelineId, cancellationToken).ConfigureAwait(false);
        return read.IsFailure
            ? read.ToNewResult<Guid?>()
            : GenericResult<Guid?>.Success(read.Value?.OrgId);
    }
}
