using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions.Health.Monitoring;
using Fdw.Web.Analytics.Clients.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Analytics.Clients;

/// <summary>
/// HTTP-backed implementation of <see cref="IHealthMonitorService"/> for use in the UI.
/// Delegates to the health API endpoints instead of running health checks directly.
/// </summary>
public sealed class HttpHealthMonitorService : IHealthMonitorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpHealthMonitorService> _logger;

    /// <summary>Initializes a new instance of <see cref="HttpHealthMonitorService"/>.</summary>
    public HttpHealthMonitorService(IHttpClientFactory httpClientFactory, ILogger<HttpHealthMonitorService>? logger = null)
    {
        _httpClient = httpClientFactory.CreateClient("HealthMonitorClient");
        _logger = logger ?? NullLogger<HttpHealthMonitorService>.Instance;
    }

    // ── IGenericService ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public string Id => nameof(HttpHealthMonitorService);

    /// <inheritdoc />
    public string Name => nameof(HttpHealthMonitorService);

    /// <inheritdoc/>
    public string ServiceType => "HealthMonitor";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(Fdw.Abstractions.IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<T>.Failure(
            Fdw.Services.Abstractions.Health.Monitoring.Logging.HealthMonitorLog.CommandsNotSupported(_logger, Id)));

    /// <inheritdoc/>
    public Task<IGenericResult> Execute(Fdw.Abstractions.IGenericCommand command, CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult.Failure(
            Fdw.Services.Abstractions.Health.Monitoring.Logging.HealthMonitorLog.CommandsNotSupported(_logger, Id)));

    /// <inheritdoc />
    public async Task<IGenericResult<SystemHealthSnapshot>> GetSystemHealth(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("health/system", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return GenericResult<SystemHealthSnapshot>.Failure(HealthMonitorClientLog.GetSystemHealthFailed(_logger, (int)response.StatusCode));

        var snapshot = await response.Content.ReadFromJsonAsync<SystemHealthSnapshot>(cancellationToken).ConfigureAwait(false);
        return GenericResult<SystemHealthSnapshot>.Success(snapshot!);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ServiceHealthSnapshot>> GetServiceHealth(string serviceName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"health/services/{Uri.EscapeDataString(serviceName)}", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return GenericResult<ServiceHealthSnapshot>.Failure(HealthMonitorClientLog.GetServiceHealthFailed(_logger, serviceName, (int)response.StatusCode));

        var snapshot = await response.Content.ReadFromJsonAsync<ServiceHealthSnapshot>(cancellationToken).ConfigureAwait(false);
        return GenericResult<ServiceHealthSnapshot>.Success(snapshot!);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ThroughputData>> GetThroughput(string serviceName, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var windowStr = FormatWindow(window);
        var response = await _httpClient.GetAsync($"health/services/{Uri.EscapeDataString(serviceName)}/throughput?window={windowStr}", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return GenericResult<ThroughputData>.Failure(HealthMonitorClientLog.GetThroughputFailed(_logger, serviceName, (int)response.StatusCode));

        var data = await response.Content.ReadFromJsonAsync<ThroughputData>(cancellationToken).ConfigureAwait(false);
        return GenericResult<ThroughputData>.Success(data!);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<HealthCheckPoint>>> GetHealthHistory(string serviceName, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var windowStr = FormatWindow(window);
        var response = await _httpClient.GetAsync($"health/services/{Uri.EscapeDataString(serviceName)}/history?window={windowStr}", cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            return GenericResult<IReadOnlyList<HealthCheckPoint>>.Failure(HealthMonitorClientLog.GetHealthHistoryFailed(_logger, serviceName, (int)response.StatusCode));

        var history = await response.Content.ReadFromJsonAsync<List<HealthCheckPoint>>(cancellationToken).ConfigureAwait(false);
        return GenericResult<IReadOnlyList<HealthCheckPoint>>.Success(history ?? []);
    }

    private static string FormatWindow(TimeSpan window)
    {
        if (window.TotalDays >= 1 && window.TotalDays == Math.Floor(window.TotalDays))
            return $"{(int)window.TotalDays}d";
        if (window.TotalHours >= 1 && window.TotalHours == Math.Floor(window.TotalHours))
            return $"{(int)window.TotalHours}h";
        return $"{(int)window.TotalMinutes}m";
    }
}
