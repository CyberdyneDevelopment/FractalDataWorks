using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Messaging;

/// <summary>
/// Service for managing access request workflows.
/// </summary>
public interface IAccessRequestService
{
    /// <summary>
    /// Creates a new access request.
    /// </summary>
    /// <param name="request">The access request details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created access request.</returns>
    Task<IGenericResult<AccessRequestPayload>> RequestAccess(CreateAccessRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves an access request.
    /// </summary>
    /// <param name="requestId">The access request identifier.</param>
    /// <param name="reviewerUserId">The reviewing user's identifier.</param>
    /// <param name="notes">Optional reviewer notes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<IGenericResult> Approve(Guid requestId, Guid reviewerUserId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Denies an access request.
    /// </summary>
    /// <param name="requestId">The access request identifier.</param>
    /// <param name="reviewerUserId">The reviewing user's identifier.</param>
    /// <param name="notes">Optional reviewer notes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<IGenericResult> Deny(Guid requestId, Guid reviewerUserId, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending access requests, optionally filtered by tenant.
    /// </summary>
    /// <param name="tenantId">Optional tenant identifier filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of pending access requests.</returns>
    Task<IGenericResult<IReadOnlyList<AccessRequestPayload>>> GetPending(Guid? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets access requests for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of access requests for the user.</returns>
    Task<IGenericResult<IReadOnlyList<AccessRequestPayload>>> GetForUser(Guid userId, CancellationToken cancellationToken = default);
}
