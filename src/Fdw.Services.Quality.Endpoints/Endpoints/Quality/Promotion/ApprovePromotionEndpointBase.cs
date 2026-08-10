using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Abstract endpoint that approves a promotion request.
/// </summary>
public abstract class ApprovePromotionEndpointBase : Endpoint<PromotionActionRequest, PromotionResponse>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovePromotionEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected ApprovePromotionEndpointBase(
        IPromotionService promotionService,
        ILogger<ApprovePromotionEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<ApprovePromotionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/promotion/requests/{Id}/approve");
        Policies("datasets:write");
        Summary(s => s.Summary = "Approve a promotion request");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Approves a promotion request.</summary>
    public override async Task HandleAsync(PromotionActionRequest req, CancellationToken ct)
    {
        PromotionEndpointLog.ApprovingPromotion(_logger, req.Id, req.ActionBy);

        try
        {
            // Why: probe existence first so an unknown Id returns 404 instead of a generic 500
            // from ApproveRequest's failure path.
            var existing = await _promotionService.GetRequest(req.Id, ct).ConfigureAwait(false);
            if (!existing.IsSuccess || existing.Value is null)
            {
                await Send.NotFoundAsync(ct).ConfigureAwait(false);
                return;
            }

            var result = await _promotionService.ApproveRequest(req.Id, req.ActionBy, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.ApprovePromotionFailed(_logger, req.Id, result.CurrentMessage!);
                AddError("Failed to approve promotion");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                AddError("Failed to approve promotion");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            PromotionEndpointLog.PromotionApproved(_logger, req.Id);
            await Send.OkAsync(ListPromotionsEndpointBase.MapToDto(result.Value), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.ApprovePromotionFailed(_logger, req.Id, ex.Message);
            AddError("Failed to approve promotion");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
