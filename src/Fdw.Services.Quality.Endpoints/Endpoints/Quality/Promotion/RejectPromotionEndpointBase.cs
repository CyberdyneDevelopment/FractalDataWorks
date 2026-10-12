using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Abstract endpoint that rejects a promotion request.
/// </summary>
public abstract class RejectPromotionEndpointBase : Endpoint<PromotionActionRequest, PromotionResponse>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RejectPromotionEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected RejectPromotionEndpointBase(
        IPromotionService promotionService,
        ILogger<RejectPromotionEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<RejectPromotionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/promotion/requests/{Id}/reject");
        Policies("datasets:write");
        Summary(s => s.Summary = "Reject a promotion request");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Rejects a promotion request.</summary>
    public override async Task HandleAsync(PromotionActionRequest req, CancellationToken ct)
    {
        PromotionEndpointLog.RejectingPromotion(_logger, req.Id, req.ActionBy);

        try
        {
            var reason = req.Comments ?? string.Empty;
            var result = await _promotionService.RejectRequest(req.Id, req.ActionBy, reason, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.RejectPromotionFailed(_logger, req.Id, result.CurrentMessage!);
                var status = (result.CurrentMessage ?? string.Empty)
                    .Contains("not found", System.StringComparison.OrdinalIgnoreCase) ? 404 : 500;
                AddError("Failed to reject promotion");
                await Send.ErrorsAsync(status, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                AddError("Promotion request was not found.");
                await Send.ErrorsAsync(404, ct).ConfigureAwait(false);
                return;
            }

            PromotionEndpointLog.PromotionRejected(_logger, req.Id);
            await Send.OkAsync(ListPromotionsEndpointBase.MapToDto(result.Value), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.RejectPromotionFailed(_logger, req.Id, ex.Message);
            AddError("Failed to reject promotion");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
