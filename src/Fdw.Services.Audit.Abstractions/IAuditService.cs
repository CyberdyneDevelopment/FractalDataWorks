using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Audit.Abstractions;

/// <summary>
/// Service for recording audit trail entries for entity operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Records a create operation.
    /// </summary>
    /// <param name="entityType">The type of entity (e.g., "Connection", "Pipeline").</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="afterState">JSON representation of the created entity.</param>
    /// <param name="context">The audit context containing caller information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> RecordCreate(
        string entityType,
        string entityId,
        string afterState,
        AuditContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an update operation.
    /// </summary>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="beforeState">JSON representation of the entity before the change.</param>
    /// <param name="afterState">JSON representation of the entity after the change.</param>
    /// <param name="changedFields">JSON array of field names that changed.</param>
    /// <param name="context">The audit context containing caller information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> RecordUpdate(
        string entityType,
        string entityId,
        string beforeState,
        string afterState,
        string? changedFields,
        AuditContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a delete operation.
    /// </summary>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="beforeState">JSON representation of the entity before deletion.</param>
    /// <param name="context">The audit context containing caller information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> RecordDelete(
        string entityType,
        string entityId,
        string beforeState,
        AuditContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the audit trail for a specific entity.
    /// </summary>
    /// <param name="entityType">The type of entity.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the audit records.</returns>
    Task<IGenericResult<AuditRecord[]>> GetAuditTrail(
        string entityType,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists audit records with optional filters.
    /// </summary>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="entityId">Optional entity ID filter.</param>
    /// <param name="action">Optional action filter (Create, Update, Delete).</param>
    /// <param name="userId">Optional user ID filter.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the matching audit records.</returns>
    Task<IGenericResult<AuditRecord[]>> ListAuditRecords(
        string? entityType,
        string? entityId,
        string? action,
        string? userId,
        int limit,
        CancellationToken cancellationToken = default);
}
