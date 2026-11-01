using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Etl.Graphs.Describing;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Reads the fields of a data set the way a graph is described (FDW-1396).</summary>
public static class GraphDataSetFields
{
    /// <summary>Reads the fields of a data set, in the order the data set states them.</summary>
    /// <param name="dataSets">The data sets.</param>
    /// <param name="name">The data set's name.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The fields, or why they could not be read.</returns>
    public static async Task<IGenericResult<IReadOnlyList<GraphField>>> Read(
        IDataSetConfigurationProvider dataSets, string name, CancellationToken cancellationToken = default)
    {
        var read = await dataSets.Get(name, ConfigurationLevels.Children, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read.ToNewResult<IReadOnlyList<GraphField>>();
        }

        return GenericResult<IReadOnlyList<GraphField>>.Success(
            [.. read.Value.Fields.Where(field => !field.IsDeleted).OrderBy(field => field.Ordinal).Select(field => new GraphField(field.Name, field.TypeName))]);
    }
}
