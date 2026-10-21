using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the data sets each data set is built from and the data stores each is held in (FDW-1683).</summary>
internal static class DataSetEdges
{
    /// <summary>Reads every data set with its sources and lists them.</summary>
    /// <param name="provider">The data set configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The edges, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEdge>>> Read(IDataSetConfigurationProvider provider, CancellationToken cancellationToken)
    {
        // Why the children: a data set's sources are its children.
        var read = await provider.Get(ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEdge>>();
        }

        var edges = new List<CatalogEdge>();
        foreach (var dataSet in read.Value!)
        {
            AddDerivedFrom(edges, dataSet.Name, dataSet.SourceDataSetName);
            foreach (var source in dataSet.Sources)
            {
                AddDerivedFrom(edges, dataSet.Name, source.SourceDataSetName);
                if (!string.IsNullOrWhiteSpace(source.DataStoreName))
                {
                    edges.Add(new CatalogEdge(CatalogKinds.DataSet, dataSet.Name, CatalogRelations.StoredIn, CatalogKinds.DataStore, source.DataStoreName, []));
                }
            }
        }

        return GenericResult<List<CatalogEdge>>.Success(Distinct(edges));
    }

    private static void AddDerivedFrom(List<CatalogEdge> edges, string dataSet, string? origin)
    {
        if (!string.IsNullOrWhiteSpace(origin) && !string.Equals(origin, dataSet, StringComparison.OrdinalIgnoreCase))
        {
            edges.Add(new CatalogEdge(CatalogKinds.DataSet, dataSet, CatalogRelations.DerivedFrom, CatalogKinds.DataSet, origin, []));
        }
    }

    // Why distinct: a data set and its sources can both name the same origin or store, and one fact is listed once.
    private static List<CatalogEdge> Distinct(List<CatalogEdge> edges)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var distinct = new List<CatalogEdge>();
        foreach (var edge in edges)
        {
            if (seen.Add($"{edge.FromName}|{edge.Relation}|{edge.ToKind}|{edge.ToName}"))
            {
                distinct.Add(edge);
            }
        }

        return distinct;
    }
}
