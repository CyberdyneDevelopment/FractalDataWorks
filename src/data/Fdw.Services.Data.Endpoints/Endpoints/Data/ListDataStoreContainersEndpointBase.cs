using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Fdw.Web.RestEndpoints.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Base endpoint for listing the containers across all data stores, one page at a time (FDW-2311).
/// </summary>
/// <remarks>
/// <c>GET /datastore-containers?q=&amp;skip=&amp;take=&amp;total=true</c>. A page is the page's rows of the containers table in container-name order and the
/// names of the path and store above each of them, so the cost of a page does not grow with the number of containers. <c>q</c> finds text in the
/// container's name and its kind in the way <c>?match=</c> names; <c>?id=</c> keeps the containers with those ids; <c>?total=true</c> adds how many
/// containers the search keeps. The containers' fields, keys and path policies are not composed to list them (FDW-951): a container's fields are
/// read on their own route, when it is opened.
/// </remarks>
public abstract class ListDataStoreContainersEndpointBase : CrudPageEndpointBase<DataStoreContainerEntry, DataStoreContainerWithPathDto>
{
    private static readonly string[] TextProperties = ["Name", "TypeId"];

    private readonly IDataStoreContainerPages _pages;

    /// <summary>Initializes a new instance of the <see cref="ListDataStoreContainersEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="pages">Reads a page of containers with the names above them.</param>
    protected ListDataStoreContainersEndpointBase(ILogger<ListDataStoreContainersEndpointBase> logger, IDataStoreContainerPages pages) : base(logger)
    {
        _pages = pages ?? throw new System.ArgumentNullException(nameof(pages));
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "datastore-containers";

    /// <inheritdoc />
    protected override string EndpointSummary => "List the containers of all data stores";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Streams one page of the containers across all data stores in container-name order, with the name of the path and the store each sits under. " +
        "?q= finds text in the container name and kind as ?match= says; ?total=true adds how many containers match in all. hasMore says whether another page follows.";

    /// <inheritdoc />
    protected override bool Searchable => true;

    /// <inheritdoc />
    protected override async Task<IGenericResult<ConfigurationPage<DataStoreContainerEntry>>> LoadPage(PaginatedListRequest paging, CancellationToken ct)
    {
        var read = await _pages.Page(Search.Filter(TextProperties, "Id")?.Root, paging.Skip, paging.Take, Search.IncludeTotal, ct).ConfigureAwait(false);
        if (read.IsFailure || read.Value is not { } page)
        {
            return read.ToNewResult<ConfigurationPage<DataStoreContainerEntry>>();
        }

        return GenericResult<ConfigurationPage<DataStoreContainerEntry>>.Success(
            new ConfigurationPage<DataStoreContainerEntry>(paging.Skip, paging.Take, page.HasMore, _ => page.Entries.ToAsyncEnumerable(), page.Total));
    }

    /// <inheritdoc />
    protected override Task<IGenericResult<DataStoreContainerWithPathDto>> ToSummary(DataStoreContainerEntry item, CancellationToken ct) =>
        Task.FromResult<IGenericResult<DataStoreContainerWithPathDto>>(GenericResult<DataStoreContainerWithPathDto>.Success(new DataStoreContainerWithPathDto
        {
            Id = item.Id,
            DataStoreName = item.DataStoreName,
            PathName = item.PathName,
            ContainerName = item.ContainerName,
            ContainerType = item.ContainerType,
            ClassificationId = item.ClassificationId,
        }));
}
