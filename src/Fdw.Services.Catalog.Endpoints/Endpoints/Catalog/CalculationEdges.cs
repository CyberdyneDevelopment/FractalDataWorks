using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Calculations;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Lists the data sets each calculation reads as inputs and writes as its output (FDW-1683).</summary>
internal static class CalculationEdges
{
    /// <summary>Reads every calculation whole and lists its data sets.</summary>
    /// <param name="provider">The calculation configuration provider.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The edges, or the failure of the read.</returns>
    internal static async Task<IGenericResult<List<CatalogEdge>>> Read(ICalculationConfigurationProvider provider, CancellationToken cancellationToken)
    {
        var read = await provider.Get(ConfigurationLevels.WholeAggregate, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure)
        {
            return read.ToNewResult<List<CatalogEdge>>();
        }

        var edges = new List<CatalogEdge>();
        foreach (var calculation in read.Value!)
        {
            foreach (var input in calculation.Inputs)
            {
                if (!string.IsNullOrWhiteSpace(input.DataSetName))
                {
                    edges.Add(new CatalogEdge(CatalogKinds.Calculation, calculation.Name, CatalogRelations.Reads, CatalogKinds.DataSet, input.DataSetName, []));
                }
            }

            if (!string.IsNullOrWhiteSpace(calculation.OutputDataSetName))
            {
                edges.Add(new CatalogEdge(CatalogKinds.Calculation, calculation.Name, CatalogRelations.Writes, CatalogKinds.DataSet, calculation.OutputDataSetName, []));
            }
        }

        return GenericResult<List<CatalogEdge>>.Success(edges);
    }
}
