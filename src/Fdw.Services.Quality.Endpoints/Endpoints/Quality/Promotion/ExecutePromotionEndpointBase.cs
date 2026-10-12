using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Abstract endpoint that executes an approved promotion request.
/// </summary>
public abstract class ExecutePromotionEndpointBase : Endpoint<ExecutePromotionRequest, PromotionResultDto>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutePromotionEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected ExecutePromotionEndpointBase(
        IPromotionService promotionService,
        ILogger<ExecutePromotionEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<ExecutePromotionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/promotion/requests/{Id}/execute");
        Policies("datasets:write");
        Summary(s => s.Summary = "Execute an approved promotion request");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Executes an approved promotion request.</summary>
    public override async Task HandleAsync(ExecutePromotionRequest req, CancellationToken ct)
    {
        PromotionEndpointLog.ExecutingPromotion(_logger, req.Id);

        try
        {
            var result = await _promotionService.ExecutePromotion(req.Id, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.ExecutePromotionFailed(_logger, req.Id, result.CurrentMessage!);
                AddError("Failed to execute promotion");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                AddError("Failed to execute promotion");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            var r = result.Value;
            PromotionEndpointLog.PromotionExecuted(_logger, req.Id, r.SuccessfulItems, r.TotalItems);
            await Send.OkAsync(new PromotionResultDto
            {
                RequestId = r.RequestId,
                SourceEnvironment = r.SourceEnvironment,
                TargetEnvironment = r.TargetEnvironment,
                TotalItems = r.TotalItems,
                SuccessfulItems = r.SuccessfulItems,
                FailedItems = r.FailedItems,
                CompletedAt = r.CompletedAt,
                Items = r.Items.Select(i => new PromotionItemResultDto
                {
                    ItemType = i.ItemType,
                    ItemName = i.ItemName,
                    Success = i.Success,
                    ErrorMessage = i.ErrorMessage
                }).ToList()
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.ExecutePromotionFailed(_logger, req.Id, ex.Message);
            AddError("Failed to execute promotion");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }
}
