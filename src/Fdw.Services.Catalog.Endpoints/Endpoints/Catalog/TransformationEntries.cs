using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the transformations as catalog entries (FDW-1681).</summary>
internal static class TransformationEntries
{
    /// <summary>Reads the transformations.</summary>
    /// <param name="provider">The transformation configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEntryDto>>> Read(ITransformationConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var read = await provider.Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEntryDto>>();
        }

        return GenericResult<List<CatalogEntryDto>>.Success(read.Value!
            .Select(transformation => new CatalogEntryDto { EntityType = CatalogKinds.Transformation, Name = transformation.Name, Description = transformation.Description })
            .ToList());
    }
}
