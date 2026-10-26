using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the data sets as catalog entries (FDW-1681). Their fields are a ranked source of their own (FDW-2312).</summary>
internal static class DataSetEntries
{
    /// <summary>Reads the data sets, without their fields.</summary>
    /// <param name="provider">The data set configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEntryDto>>> Read(
        IDataSetConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var read = await provider.Get(ConfigurationLevels.RecordOnly, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEntryDto>>();
        }

        var entries = new List<CatalogEntryDto>();
        foreach (var dataSet in read.Value!)
        {
            entries.Add(new CatalogEntryDto
            {
                EntityType = CatalogKinds.DataSet,
                Name = dataSet.Name,
                Description = string.IsNullOrWhiteSpace(dataSet.Description) ? dataSet.DisplayName : dataSet.Description,
            });
        }

        return GenericResult<List<CatalogEntryDto>>.Success(entries);
    }
}
