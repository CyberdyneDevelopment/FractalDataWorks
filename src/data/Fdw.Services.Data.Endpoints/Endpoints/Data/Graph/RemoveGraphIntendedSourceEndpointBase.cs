using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>DELETE /datasets/graph/{name}/intended-sources/{sourceId}</c> (FDW-5452): removes one intended source from a Graph data set. The other intended sources are left as they are.
/// </summary>
/// <remarks>Whoever may update a data set may remove one: the route asks the update permission (<c>datasets:write</c>), no wider than replacing the list by an update.</remarks>
public abstract class RemoveGraphIntendedSourceEndpointBase : CrudDeleteEndpointBase<GraphIntendedSourceRouteRequest>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="RemoveGraphIntendedSourceEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves data sets.</param>
    protected RemoveGraphIntendedSourceEndpointBase(ILogger<RemoveGraphIntendedSourceEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string DeletePolicy => "datasets:write";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/{Name}/intended-sources/{SourceId}";

    /// <inheritdoc />
    protected override string EndpointSummary => "Remove an intended source from a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription => "Removes one intended source. The data set's other intended sources are left as they are.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(GraphIntendedSourceRouteRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(GraphIntendedSourceRouteRequest request, CancellationToken ct)
    {
        var found = await _dataSetProvider.Get(request.Name, ct).ConfigureAwait(false);
        return found.IsFailure ? found.ToNewResult<bool>() : GenericResult<bool>.Success(found.Value is not null);
    }

    /// <inheritdoc />
    protected override Task<IGenericResult> Delete(GraphIntendedSourceRouteRequest request, CancellationToken ct)
        => GraphDataSetEdits.RemoveIntendedSource(_dataSetProvider, request, Logger, ct);
}
