using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Pipelines.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the pipelines as catalog entries (FDW-1681).</summary>
internal static class PipelineEntries
{
    /// <summary>Reads the pipelines.</summary>
    /// <param name="provider">The pipeline configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEntryDto>>> Read(IPipelineConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var read = await provider.Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEntryDto>>();
        }

        return GenericResult<List<CatalogEntryDto>>.Success(read.Value!
            .Select(pipeline => new CatalogEntryDto
            {
                EntityType = CatalogKinds.Pipeline,
                Name = pipeline.Name,
                Description = pipeline.Description,
                LastModified = pipeline.ModifyDate.UtcDateTime,
            })
            .ToList());
    }
}
