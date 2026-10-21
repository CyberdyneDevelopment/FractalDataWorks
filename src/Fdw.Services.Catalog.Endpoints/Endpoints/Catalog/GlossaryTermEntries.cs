using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Quality;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the glossary terms as catalog entries, a term's definition being its description (FDW-1681).</summary>
internal static class GlossaryTermEntries
{
    /// <summary>Reads the glossary terms.</summary>
    /// <param name="provider">The glossary term configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEntryDto>>> Read(IGlossaryTermConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var read = await provider.Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEntryDto>>();
        }

        return GenericResult<List<CatalogEntryDto>>.Success(read.Value!
            .Select(term => new CatalogEntryDto
            {
                EntityType = CatalogKinds.GlossaryTerm,
                Name = term.Name,
                Description = term.StandsFor is { Length: > 0 } standsFor ? string.Concat(standsFor, ". ", term.Definition) : term.Definition,
                Owner = string.IsNullOrWhiteSpace(term.Owner) ? null : term.Owner,
            })
            .ToList());
    }
}
