using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions.DataOptionRows;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Lists the option rows of a container (FDW-5664):
/// <c>GET /datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options?column=&amp;skip=&amp;take=</c>.
/// </summary>
/// <remarks>
/// A store-paged list, sized by the platform's page settings like every other list: the service reads one page with one row
/// past it, so <c>hasMore</c> says whether another follows. Rows come in column then value order.
/// </remarks>
public abstract class ListDataOptionRowsEndpointBase : CrudPageEndpointBase<DataOptionRow, DataOptionRowPayload>
{
    private readonly IDataOptionRows _rows;

    /// <summary>Initializes a new instance of the <see cref="ListDataOptionRowsEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="rows">Reads the option rows.</param>
    protected ListDataOptionRowsEndpointBase(ILogger<ListDataOptionRowsEndpointBase> logger, IDataOptionRows rows) : base(logger)
    {
        _rows = rows;
    }

    /// <summary>Gets the resource name used for the documentation tag.</summary>
    protected override string ResourceName => "datastores";

    /// <summary>Gets the read policy: anyone who can read the data store may see its option rows.</summary>
    protected override string ReadPolicy => "datastores:read";

    /// <summary>Gets the nested-resource route.</summary>
    protected override string Route => "/datastores/{Name}/paths/{PathName}/containers/{ContainerName}/options";

    /// <summary>Gets the summary shown for the route.</summary>
    protected override string EndpointSummary => "List the option rows of a container";

    /// <summary>Gets the description shown for the route.</summary>
    protected override string EndpointDescription =>
        "Streams one page of the container's option rows in column then value order, each with its id; ?column= keeps one column's rows. hasMore says whether another page follows.";

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<DataOptionRow>>> LoadPage(PaginatedListRequest paging, CancellationToken ct)
    {
        string[] names = ["Name", "PathName", "ContainerName"];
        var values = new List<string>();
        foreach (var name in names)
        {
            if (HttpContext.Request.RouteValues[name] is not string { Length: > 0 } value)
                return GenericResult<ConfigurationPage<DataOptionRow>>.Failure(DataOptionRowsEndpointLog.RouteValueMissing(Logger, name));

            values.Add(value);
        }

        var column = HttpContext.Request.Query["column"].ToString();
        var read = await _rows.List(
            new DataOptionRowsQuery
            {
                Target = DataOptionRowMapping.Target(values[0], values[1], values[2]),
                Column = column.Length == 0 ? null : column,
                Skip = paging.Skip,
                Take = paging.Take,
            },
            ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } page)
            return read.ToNewResult<ConfigurationPage<DataOptionRow>>();

        return GenericResult<ConfigurationPage<DataOptionRow>>.Success(
            new ConfigurationPage<DataOptionRow>(paging.Skip, paging.Take, page.HasMore, _ => page.Items.ToAsyncEnumerable()));
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<DataOptionRowPayload>> ToSummary(DataOptionRow item, CancellationToken ct)
        => Task.FromResult<IGenericResult<DataOptionRowPayload>>(GenericResult<DataOptionRowPayload>.Success(DataOptionRowMapping.ToPayload(item)));
}
