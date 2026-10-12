namespace Fdw.Web.Analytics.Clients.ApiClients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Analytics.Clients.Models;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for promotion and environment management endpoints.
/// </summary>
public sealed class PromotionApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public PromotionApiClient(HttpClient httpClient, ILogger<PromotionApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all available environments.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of environments.</returns>
    public Task<IGenericResult<IReadOnlyList<EnvironmentPayload>>> GetEnvironments(CancellationToken ct = default)
        => GetList<EnvironmentPayload>("admin/environments", ct);

    /// <summary>
    /// Gets all promotions regardless of status.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of all promotions.</returns>
    public Task<IGenericResult<IReadOnlyList<PromotionPayload>>> GetAllPromotions(CancellationToken ct = default)
        => GetList<PromotionPayload>("promotion/requests", ct);

    /// <summary>
    /// Gets all pending promotions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of pending promotions.</returns>
    public Task<IGenericResult<IReadOnlyList<PromotionPayload>>> GetPendingPromotions(CancellationToken ct = default)
        => GetList<PromotionPayload>("promotion/requests?status=Pending", ct);

    /// <summary>
    /// Gets a specific promotion by its identifier.
    /// </summary>
    /// <param name="id">The promotion identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the promotion detail.</returns>
    public Task<IGenericResult<PromotionPayload>> GetPromotion(Guid id, CancellationToken ct = default)
        => Get<PromotionPayload>($"promotion/requests/{id}", ct);

    /// <summary>
    /// Creates a new promotion.
    /// </summary>
    /// <param name="request">The create promotion request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created promotion.</returns>
    public Task<IGenericResult<PromotionPayload>> CreatePromotion(CreatePromotionPayload request, CancellationToken ct = default)
        => Post<CreatePromotionPayload, PromotionPayload>("promotion/requests", request, ct);

    /// <summary>
    /// Approves a pending promotion.
    /// </summary>
    /// <param name="id">The promotion identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the approval.</returns>
    public Task<IGenericResult> ApprovePromotion(Guid id, CancellationToken ct = default)
        => Post($"promotion/requests/{id}/approve", (object?)null, ct);

    /// <summary>
    /// Rejects a pending promotion.
    /// </summary>
    /// <param name="id">The promotion identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating success or failure of the rejection.</returns>
    public Task<IGenericResult> RejectPromotion(Guid id, CancellationToken ct = default)
        => Post($"promotion/requests/{id}/reject", (object?)null, ct);
}
