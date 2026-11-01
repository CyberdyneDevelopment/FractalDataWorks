using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Quality;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the data sets, and the fields of them, that each glossary term explains (FDW-1683).</summary>
internal static class GlossaryTermEdges
{
    /// <summary>Reads every glossary term whole and lists its linked data sets.</summary>
    /// <param name="provider">The glossary term configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The edges, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEdge>>> Read(IGlossaryTermConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var read = await provider.Get(ConfigurationLevels.WholeAggregate, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEdge>>();
        }

        var edges = new List<CatalogEdge>();
        foreach (var term in read.Value!)
        {
            foreach (var link in term.LinkedDataSets)
            {
                // A link that names a field explains that field; one that names none explains the data set.
                edges.Add(string.IsNullOrWhiteSpace(link.FieldName)
                    ? new CatalogEdge(CatalogKinds.GlossaryTerm, term.Name, CatalogRelations.Describes, CatalogKinds.DataSet, link.DataSetName, [])
                    : new CatalogEdge(CatalogKinds.GlossaryTerm, term.Name, CatalogRelations.Describes, CatalogKinds.DataSetField, link.FieldName, [link.DataSetName]));
            }
        }

        return GenericResult<List<CatalogEdge>>.Success(edges);
    }
}
