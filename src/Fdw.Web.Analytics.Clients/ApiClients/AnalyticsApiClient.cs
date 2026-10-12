namespace Fdw.Web.Analytics.Clients.ApiClients;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for analytics endpoints.
/// </summary>
public class AnalyticsApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyticsApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public AnalyticsApiClient(HttpClient httpClient, ILogger<AnalyticsApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets analytics data.
    /// </summary>
    /// <param name="request">The analytics request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the analytics response.</returns>
    public virtual Task<IGenericResult<AnalyticsResponse>> GetAnalytics(AnalyticsRequest request, CancellationToken ct = default)
    {
        var path = $"analytics?startDate={Uri.EscapeDataString(request.StartDate.ToString("O"))}&endDate={Uri.EscapeDataString(request.EndDate.ToString("O"))}";
        if (!string.IsNullOrEmpty(request.CalculationType))
        {
            path += $"&calculationType={Uri.EscapeDataString(request.CalculationType)}";
        }

        return Get<AnalyticsResponse>(path, ct);
    }

    /// <summary>
    /// Gets the top calculations by usage.
    /// </summary>
    /// <param name="request">The top calculations request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the top calculations response.</returns>
    public virtual Task<IGenericResult<TopCalculationsResponse>> GetTopCalculations(TopCalculationsRequest request, CancellationToken ct = default)
    {
        var path = $"analytics/top-calculations?count={request.Count}&since={Uri.EscapeDataString(request.Since.ToString("O"))}";
        return Get<TopCalculationsResponse>(path, ct);
    }
}
