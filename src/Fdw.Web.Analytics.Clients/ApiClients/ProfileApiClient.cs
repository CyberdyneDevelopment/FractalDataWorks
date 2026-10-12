namespace Fdw.Web.Analytics.Clients.ApiClients;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for data profiling endpoints.
/// </summary>
public sealed class ProfileApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public ProfileApiClient(HttpClient httpClient, ILogger<ProfileApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets the data profile for a DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the data profile.</returns>
    public Task<IGenericResult<DataProfilePayload>> GetProfile(string dataSetName, CancellationToken ct = default)
        => Get<DataProfilePayload>($"datasets/{dataSetName}/profile", ct);

    /// <summary>
    /// Runs profiling on a DataSet.
    /// </summary>
    /// <param name="dataSetName">The DataSet name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the generated data profile.</returns>
    public Task<IGenericResult<DataProfilePayload>> RunProfile(string dataSetName, CancellationToken ct = default)
        => PostWithResponse<DataProfilePayload>($"datasets/{dataSetName}/profile", ct);
}
