using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Abstract endpoint that lists all configured deployment environments.
/// </summary>
public abstract class ListEnvironmentsEndpointBase : EndpointWithoutRequest<IReadOnlyList<EnvironmentResponse>>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListEnvironmentsEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected ListEnvironmentsEndpointBase(
        IPromotionService promotionService,
        ILogger<ListEnvironmentsEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<ListEnvironmentsEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/promotion/environments");
        Policies("datasets:read");
        Summary(s => s.Summary = "List environments");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Lists all configured deployment environments.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        PromotionEndpointLog.ListingEnvironments(_logger);

        try
        {
            var result = await _promotionService.GetEnvironments(ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.ListEnvironmentsFailed(_logger, result.CurrentMessage!);
                AddError("Failed to list environments");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                await Send.OkAsync(new List<EnvironmentResponse>(), ct).ConfigureAwait(false);
                return;
            }

            var environments = result.Value.Select(e => new EnvironmentResponse
            {
                Id = e.Id,
                Name = e.Name,
                Order = e.PromotionOrder,
                ConnectionName = e.ConnectionName,
                RequiresApproval = e.RequiresApproval,
                Approvers = e.Approvers.Select(a => a.ApproverName).ToList(),
                Description = e.Description
            }).ToList();

            PromotionEndpointLog.EnvironmentsFound(_logger, environments.Count);
            await Send.OkAsync(environments, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.ListEnvironmentsFailed(_logger, ex.Message);
            AddError("Failed to list environments");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
