using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Etl.Graphs.Describing;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Reads the fields of a container the way a graph is described (FDW-5523): from the data store's stored containers, as the check before save reads them.</summary>
public static class GraphContainerFields
{
    /// <summary>Reads the fields of the container a ContainerSource states, in the order the container states them.</summary>
    /// <param name="sources">Reads the stored data stores and containers.</param>
    /// <param name="request">The component and what it states: data store, path (or none) and container.</param>
    /// <param name="logger">The logger a failure's result code is written to.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The fields, or why they could not be read: the data store or container is not there, the path does not hold it, or it is in several paths and none is stated.</returns>
    public static async Task<IGenericResult<IReadOnlyList<GraphField>>> Read(
        IGraphDataSetSources sources, ContainerFieldsRequest request, ILogger logger, CancellationToken cancellationToken = default)
    {
        var read = await sources.ContainerFields(request.Component, request.DataStore, request.Path, request.Container, logger, cancellationToken).ConfigureAwait(false);
        if (read.IsFailure || read.Value is null)
        {
            return read.ToNewResult<IReadOnlyList<GraphField>>();
        }

        return GenericResult<IReadOnlyList<GraphField>>.Success([.. read.Value.Select(field => new GraphField(field.Name, field.TypeName))]);
    }
}
