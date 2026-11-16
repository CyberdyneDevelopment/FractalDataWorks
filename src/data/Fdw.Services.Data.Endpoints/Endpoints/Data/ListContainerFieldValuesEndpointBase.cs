using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions.ColumnValues;
using Fdw.Services.Data.Clients.Models;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Searches the distinct values of one column of a data store container (FDW-5666):
/// <c>GET /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/fields/{FieldName}/values?q=&amp;skip=&amp;take=&amp;chosen=&amp;leaveOutChosen=</c>.
/// </summary>
/// <remarks>
/// Reading a container's values is reading the data store, so it takes <c>datastores:read</c>. The store does the search and the
/// paging (<see cref="IContainerColumnValues"/>); this route only maps the request and the page.
/// </remarks>
public abstract class ListContainerFieldValuesEndpointBase : CrudGetEndpointBase<ContainerFieldValuesRequest, ContainerFieldValuesPayload>
{
    private readonly IContainerColumnValues _values;

    /// <summary>Initializes a new instance of the <see cref="ListContainerFieldValuesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="values">Searches a column's values on the store.</param>
    protected ListContainerFieldValuesEndpointBase(ILogger<ListContainerFieldValuesEndpointBase> logger, IContainerColumnValues values) : base(logger)
    {
        _values = values;
    }

    /// <summary>Gets the resource name used for the documentation tag.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the read policy: the values are the data store's own.</summary>
    protected override string ReadPolicy => "datastores:read";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/fields/{FieldName}/values";

    /// <summary>Gets whether an entity tag is sent: no, the values follow the container's data, which the tag does not track.</summary>
    protected override bool ETagEnabled => false;

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "Search the values of a container column";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription =>
        "Returns one page of the distinct values of a column that contain q, in value order, each with how many rows hold it and whether it is chosen, and whether more match.";

    /// <summary>Returns the data store, path, container and column as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ContainerFieldValuesRequest request)
        => $"{request.Name}/{request.PathName}/{request.ContainerName}/{request.FieldName}";

    /// <summary>Searches the values and maps the page.</summary>
    protected override async Task<IGenericResult<ContainerFieldValuesPayload?>> FindByIdentifier(ContainerFieldValuesRequest request, CancellationToken ct)
    {
        var page = await _values.Search(
            new ColumnValuesQuery
            {
                DataStore = request.Name,
                Path = request.PathName,
                Container = request.ContainerName,
                Column = request.FieldName,
                Search = request.Q,
                Skip = request.Skip,
                Take = request.Take,
                Chosen = request.Chosen,
                LeaveOutChosen = request.LeaveOutChosen,
            },
            ct).ConfigureAwait(false);
        if (page.IsFailure || page.Value is null)
            return page.ToNewResult<ContainerFieldValuesPayload?>();

        return GenericResult<ContainerFieldValuesPayload?>.Success(new ContainerFieldValuesPayload
        {
            Items = [.. page.Value.Items.Select(v => new ContainerFieldValuePayload { Value = v.Value, Rows = v.Rows, Chosen = v.Chosen })],
            HasMore = page.Value.HasMore,
        });
    }
}
