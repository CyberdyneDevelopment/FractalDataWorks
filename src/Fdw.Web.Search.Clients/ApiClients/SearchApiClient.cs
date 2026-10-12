namespace Fdw.Web.Search.Clients.ApiClients;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Fdw.Web.Search.Clients.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for search endpoints.
/// </summary>
public sealed class SearchApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public SearchApiClient(HttpClient httpClient, ILogger<SearchApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Performs a search query.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the search response.</returns>
    public Task<IGenericResult<SearchResponse>> Search(SearchRequest request, CancellationToken ct = default)
        => Get<SearchResponse>($"search?query={System.Uri.EscapeDataString(request.Query)}&limit={request.Limit}", ct);

    /// <summary>
    /// Performs a cross-field find within a container.
    /// </summary>
    /// <param name="request">The find request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the find response.</returns>
    public Task<IGenericResult<FindResponse>> Find(FindRequest request, CancellationToken ct = default)
        => Post<FindRequest, FindResponse>("find", request, ct);
}
