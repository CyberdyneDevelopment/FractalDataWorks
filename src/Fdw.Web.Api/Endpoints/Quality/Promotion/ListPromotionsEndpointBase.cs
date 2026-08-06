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
/// Abstract endpoint that lists promotion requests, optionally filtered by status.
/// </summary>
public abstract class ListPromotionsEndpointBase : EndpointWithoutRequest<IReadOnlyList<PromotionResponse>>
{
    private readonly IPromotionService _promotionService;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPromotionsEndpointBase"/> class.
    /// </summary>
    /// <param name="promotionService">The promotion service.</param>
    /// <param name="logger">The logger instance.</param>
    protected ListPromotionsEndpointBase(
        IPromotionService promotionService,
        ILogger<ListPromotionsEndpointBase>? logger)
    {
        _promotionService = promotionService;
        _logger = logger ?? NullLogger<ListPromotionsEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/promotion/requests");
        // Why: listing promotion requests is a read-shaped operation. Viewer needs visibility into
        // pending promotions to know what's queued; write/approve are separately gated.
        Policies("datasets:read");
        Summary(s => s.Summary = "List promotion requests");
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or additional endpoint configuration.</summary>
    protected virtual void ConfigureEndpoint() { }

    /// <summary>Lists all promotion requests.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        PromotionEndpointLog.ListingPromotions(_logger);

        try
        {
            var result = await _promotionService.GetRequests(null, ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PromotionEndpointLog.ListPromotionsFailed(_logger, result.CurrentMessage!);
                AddError("Failed to list promotions");
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }

            if (result.Value is null)
            {
                await Send.OkAsync(new List<PromotionResponse>(), ct).ConfigureAwait(false);
                return;
            }

            var promotions = result.Value.Select(MapToDto).ToList();

            PromotionEndpointLog.PromotionsFound(_logger, promotions.Count);
            await Send.OkAsync(promotions, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            PromotionEndpointLog.ListPromotionsFailed(_logger, ex.Message);
            AddError("Failed to list promotions");
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Maps a promotion request configuration to a DTO.</summary>
    public static PromotionResponse MapToDto(Configuration.PromotionRequestConfiguration p)
    {
        return new PromotionResponse
        {
            Id = p.Id,
            Name = p.Name,
            SourceEnvironment = p.SourceEnvironment,
            TargetEnvironment = p.TargetEnvironment,
            Items = p.Items.Select(i => new PromotionItemDto
            {
                Id = i.Id,
                Name = i.Name,
                ItemType = i.ItemType,
                ItemName = i.ItemName,
                ItemId = i.ItemId
            }).ToList(),
            Status = p.Status,
            RequestedBy = p.RequestedBy,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            ApprovedBy = p.ApprovedBy,
            ApprovedAt = p.ApprovedAt,
            CompletedAt = p.CompletedAt
        };
    }
}
