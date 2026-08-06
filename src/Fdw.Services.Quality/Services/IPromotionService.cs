using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Quality.Configuration;

namespace Fdw.Services.Quality.Services;

/// <summary>
/// Service interface for environment promotion operations.
/// </summary>
public interface IPromotionService
{
    /// <summary>
    /// Gets all configured environments.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the list of environment configurations.</returns>
    Task<IGenericResult<IReadOnlyList<EnvironmentConfiguration>>> GetEnvironments(CancellationToken ct = default);

    /// <summary>
    /// Creates a new promotion request.
    /// </summary>
    /// <param name="request">The promotion request configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the created request configuration.</returns>
    Task<IGenericResult<PromotionRequestConfiguration>> CreateRequest(PromotionRequestConfiguration request, CancellationToken ct = default);

    /// <summary>
    /// Gets a promotion request by identifier.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the request configuration.</returns>
    Task<IGenericResult<PromotionRequestConfiguration>> GetRequest(Guid requestId, CancellationToken ct = default);

    /// <summary>
    /// Gets promotion requests filtered by status.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing matching promotion requests.</returns>
    Task<IGenericResult<IReadOnlyList<PromotionRequestConfiguration>>> GetRequests(string? status, CancellationToken ct = default);

    /// <summary>
    /// Approves a promotion request.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="approvedBy">The username of the approver.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the approved request configuration.</returns>
    Task<IGenericResult<PromotionRequestConfiguration>> ApproveRequest(Guid requestId, string approvedBy, CancellationToken ct = default);

    /// <summary>
    /// Rejects a promotion request.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="rejectedBy">The username of the rejector.</param>
    /// <param name="reason">The reason for rejection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the rejected request configuration.</returns>
    Task<IGenericResult<PromotionRequestConfiguration>> RejectRequest(Guid requestId, string rejectedBy, string reason, CancellationToken ct = default);

    /// <summary>
    /// Executes a promotion request.
    /// </summary>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the promotion execution result.</returns>
    Task<IGenericResult<PromotionResult>> ExecutePromotion(Guid requestId, CancellationToken ct = default);

    /// <summary>
    /// Compares configuration between two environments.
    /// </summary>
    /// <param name="sourceEnvironment">The source environment name.</param>
    /// <param name="targetEnvironment">The target environment name.</param>
    /// <param name="entityType">The entity type to compare.</param>
    /// <param name="entityName">The entity name to compare.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the configuration differences.</returns>
    Task<IGenericResult<ConfigDiff>> CompareEnvironments(string sourceEnvironment, string targetEnvironment, string entityType, string entityName, CancellationToken ct = default);
}