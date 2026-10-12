using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Web.Analytics.Clients;
using Fdw.Web.Analytics.Clients.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.Logging;

namespace Fdw.Operations.Endpoints.Analytics;

/// <summary>
/// Base endpoint for getting analytics summary for a time period.
/// Route: GET /analytics
/// </summary>
public abstract class GetAnalyticsEndpointBase : Endpoint<AnalyticsRequest, AnalyticsResponse>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<GetAnalyticsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAnalyticsEndpointBase"/> class.
    /// </summary>
    protected GetAnalyticsEndpointBase(IAnalyticsService analyticsService, ILogger<GetAnalyticsEndpointBase> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger ?? NullLogger<GetAnalyticsEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/analytics");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get analytics summary";
            s.Description = "Returns execution statistics, performance metrics, and usage trends for a time period.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(AnalyticsRequest req, CancellationToken ct)
    {
        try
        {
            EndpointLog.ListingResources(_logger, "analytics");

            var result = await _analyticsService.GetAnalytics(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EndpointLog.OperationFailed(_logger, ex, "get", "analytics", "summary");
            throw;
        }
    }
}
