using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the data sets, and their fields, as catalog entries (FDW-1681).</summary>
internal static class DataSetEntries
{
    /// <summary>Reads the data sets and, when fields are wanted, the fields under them.</summary>
    /// <param name="provider">The data set configuration provider.</param>
    /// <param name="kinds">The kinds the search was asked for.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The entries, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEntryDto>>> Read(
        IDataSetConfigurationProvider provider, CatalogKindSet kinds, CancellationToken cancellationToken)
    {
        // Why the level: a data set's fields are its children; reading them costs a query per data set, so only a search for fields pays it.
        var level = kinds.Wants(CatalogKinds.DataSetField) ? ConfigurationLevels.Children : ConfigurationLevels.RecordOnly;
        var read = await provider.Get(level, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEntryDto>>();
        }

        var entries = new List<CatalogEntryDto>();
        foreach (var dataSet in read.Value!)
        {
            if (kinds.Wants(CatalogKinds.DataSet))
            {
                entries.Add(new CatalogEntryDto
                {
                    EntityType = CatalogKinds.DataSet,
                    Name = dataSet.Name,
                    Description = string.IsNullOrWhiteSpace(dataSet.Description) ? dataSet.DisplayName : dataSet.Description,
                });
            }

            if (kinds.Wants(CatalogKinds.DataSetField))
            {
                foreach (var field in dataSet.Fields)
                {
                    entries.Add(new CatalogEntryDto
                    {
                        EntityType = CatalogKinds.DataSetField,
                        Name = field.Name,
                        Description = field.Description,
                        Location = dataSet.Name,
                        Parents = [dataSet.Name],
                    });
                }
            }
        }

        return GenericResult<List<CatalogEntryDto>>.Success(entries);
    }
}
