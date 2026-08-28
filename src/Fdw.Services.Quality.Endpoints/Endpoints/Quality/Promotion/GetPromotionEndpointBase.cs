using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Abstract endpoint that retrieves a promotion request by identifier.
/// </summary>
public abstract class GetPromotionEndpointBase : Endpoint<PromotionIdRequest, PromotionResponse>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPromotionEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetPromotionEndpointBase(
        IPromotionService promotionService,
        ILogger<GetPromotionEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<GetPromotionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/promotion/requests/{Id}");
        Policies("datasets:read");
        Summary(s => s.Summary = "Get promotion request by ID");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Gets a promotion request by its identifier.</summary>
    public override async Task HandleAsync(PromotionIdRequest req, CancellationToken ct)
    {
        PromotionEndpointLog.GettingPromotion(_logger, req.Id);

        try
        {
            var result = await _promotionService.GetRequest(req.Id, ct).ConfigureAwait(false);

            var msg = result.CurrentMessage ?? string.Empty;

            if ((!result.IsSuccess && msg.Contains("not found", System.StringComparison.OrdinalIgnoreCase))
                || (result.IsSuccess && result.Value is null))
            {
                PromotionEndpointLog.PromotionNotFound(_logger, req.Id);
                HttpContext.Response.StatusCode = 404;
                HttpContext.Response.ContentType = "application/json";
                await HttpContext.Response.WriteAsJsonAsync(new
                {
                    errorCode = "NotFound",
                    messages = new[] { $"Promotion '{req.Id}' was not found." }
                }, ct).ConfigureAwait(false);
                return;
            }

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.GetPromotionFailed(_logger, req.Id, msg);
                AddError("Failed to get promotion");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            await Send.OkAsync(ListPromotionsEndpointBase.MapToDto(result.Value!), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.GetPromotionFailed(_logger, req.Id, ex.Message);
            AddError("Failed to get promotion");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
