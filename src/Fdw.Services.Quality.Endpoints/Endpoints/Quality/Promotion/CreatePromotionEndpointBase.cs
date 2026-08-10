using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Quality.Endpoints.Promotion;

/// <summary>
/// Abstract endpoint that creates a new promotion request.
/// </summary>
public abstract class CreatePromotionEndpointBase : Endpoint<CreatePromotionRequest, PromotionResponse>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePromotionEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected CreatePromotionEndpointBase(
        IPromotionService promotionService,
        ILogger<CreatePromotionEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<CreatePromotionEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/promotion/requests");
        Policies("datasets:write");
        Summary(s => s.Summary = "Create a promotion request");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Creates a new promotion request.</summary>
    public override async Task HandleAsync(CreatePromotionRequest req, CancellationToken ct)
    {
        PromotionEndpointLog.CreatingPromotion(_logger, req.SourceEnvironment, req.TargetEnvironment, req.RequestedBy);

        try
        {
            var requestConfig = new PromotionRequestConfiguration
            {
                Name = req.Name,
                SourceEnvironment = req.SourceEnvironment,
                TargetEnvironment = req.TargetEnvironment,
                RequestedBy = req.RequestedBy,
                Notes = req.Notes,
                Status = "Pending",
                Items = req.Items.Select(i => new PromotionRequestItemConfiguration
                {
                    ItemType = i.ItemType,
                    ItemName = i.ItemName,
                    ItemId = i.ItemId,
                    Name = i.ItemName
                }).ToList()
            };

            var result = await _promotionService.CreateRequest(requestConfig, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.CreatePromotionFailed(_logger, result.CurrentMessage!);
                AddError("Failed to create promotion");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                AddError("Failed to create promotion");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            PromotionEndpointLog.PromotionCreated(_logger, result.Value.Id);
            await SendCreatedAtResponse(result.Value, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.CreatePromotionFailed(_logger, ex.Message);
            AddError("Failed to create promotion");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Sends the created-at response. Override to customize the response location.</summary>
    protected virtual Task SendCreatedAtResponse(PromotionRequestConfiguration p, CancellationToken ct)
    {
        return Send.CreatedAtAsync<GetPromotionEndpointBase>(
            new { p.Id },
            ListPromotionsEndpointBase.MapToDto(p),
            cancellation: ct);
    }
}
