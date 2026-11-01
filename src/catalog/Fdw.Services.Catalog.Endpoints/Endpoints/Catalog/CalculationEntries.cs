using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the calculations as catalog entries (FDW-1681).</summary>
internal static class CalculationEntries
{
    /// <summary>Reads the calculations.</summary>
    /// <param name="provider">The calculation configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEntryDto>>> Read(ICalculationConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var read = await provider.Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEntryDto>>();
        }

        return GenericResult<List<CatalogEntryDto>>.Success(read.Value!
            .Select(calculation => new CatalogEntryDto { EntityType = CatalogKinds.Calculation, Name = calculation.Name, Description = calculation.Description })
            .ToList());
    }
}
