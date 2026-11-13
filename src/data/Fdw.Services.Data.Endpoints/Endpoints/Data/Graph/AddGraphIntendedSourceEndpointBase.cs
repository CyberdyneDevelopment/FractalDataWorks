using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>POST /datasets/graph/{name}/intended-sources</c> (FDW-5452): adds one intended source to an abstract Graph data set and answers it, with its stable id, its author and when
/// it was stated. The other intended sources are left as they are.
/// </summary>
/// <remarks>
/// A graph that has components states no intended sources, and a source the data set already intends is refused by name. A data set it names must exist; a data store need
/// not (it may be external). A caller that cannot be named is refused, never stored as no author.
/// </remarks>
public abstract class AddGraphIntendedSourceEndpointBase : CrudCreateEndpointBase<AddGraphIntendedSourceRequest, GraphIntendedSourceRequest>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;

    /// <summary>Initializes a new instance of the <see cref="AddGraphIntendedSourceEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads and saves data sets.</param>
    protected AddGraphIntendedSourceEndpointBase(ILogger<AddGraphIntendedSourceEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/{Name}/intended-sources";

    /// <inheritdoc />
    protected override string EndpointSummary => "Add an intended source to a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription => "Adds one data store or data set an abstract Graph data set intends to read, and answers it with its id, author and time. The other intended sources are left as they are.";

    /// <inheritdoc />
    protected override string GetResourceName(AddGraphIntendedSourceRequest request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(AddGraphIntendedSourceRequest request, CancellationToken ct)
        => Task.FromResult<IGenericResult<bool>>(GenericResult<bool>.Success(false)); // A repeat is refused by its own code in Create, not as a conflict of the route.

    /// <inheritdoc />
    protected override Task<IGenericResult<GraphIntendedSourceRequest>> Create(AddGraphIntendedSourceRequest request, CancellationToken ct)
        => GraphDataSetEdits.AddIntendedSource(_dataSetProvider, request, GraphDataSetStamp.Saving(User), Logger, ct);
}
