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
/// Base endpoint for getting top calculations by usage.
/// Route: GET /analytics/top
/// </summary>
public abstract class GetTopCalculationsEndpointBase : Endpoint<TopCalculationsRequest, TopCalculationsResponse>
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<GetTopCalculationsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTopCalculationsEndpointBase"/> class.
    /// </summary>
    protected GetTopCalculationsEndpointBase(IAnalyticsService analyticsService, ILogger<GetTopCalculationsEndpointBase> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger ?? NullLogger<GetTopCalculationsEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/analytics/top");
        Policies("pipelines:read");
        Summary(s =>
        {
            s.Summary = "Get top calculations";
            s.Description = "Returns the most frequently used calculation types.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add Tags or other endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <inheritdoc/>
    public override async Task HandleAsync(TopCalculationsRequest req, CancellationToken ct)
    {
        try
        {
            EndpointLog.ListingResources(_logger, "top-calculations");

            var result = await _analyticsService.GetTopCalculations(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(result.Value!, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            EndpointLog.OperationFailed(_logger, ex, "get", "analytics", "top-calculations");
            throw;
        }
    }
}
