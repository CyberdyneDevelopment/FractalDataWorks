using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Execution;

/// <summary>
/// Service for tracking execution of workflows, jobs, and other hierarchical items.
/// </summary>
/// <remarks>
/// Every item belongs to one tenant, and every read and control is scoped to the caller: a system
/// context reaches every item, a caller acting in the global tenant reaches every item, a caller acting
/// in a tenant reaches that tenant's items only, and a caller with no tenant reaches none. An item the
/// caller may not reach answers exactly as an item that does not exist.
/// </remarks>
public interface IExecutionTracker
{
    /// <summary>
    /// Creates a new execution item.
    /// </summary>
    /// <param name="itemType">The type of execution item.</param>
    /// <param name="name">The name of the execution item.</param>
    /// <param name="tenantId">
    /// The tenant the execution belongs to. Required for a root item: an item without a tenant is refused,
    /// never stored as everybody's. A child always belongs to its parent's tenant, so for a child this is
    /// either null or that same tenant; a different tenant is refused. A caller acting in one tenant may
    /// only create items for that tenant.
    /// </param>
    /// <param name="parentId">The parent execution item ID, if any.</param>
    /// <param name="correlationId">The correlation ID for distributed tracing.</param>
    /// <param name="triggerSource">The source that triggered this execution.</param>
    /// <param name="parameters">The execution parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created execution item.</returns>
    Task<IGenericResult<IExecutionItem>> CreateItem(
        IExecutionItemType itemType,
        string name,
        Guid? tenantId,
        Guid? parentId = null,
        string? correlationId = null,
        string? triggerSource = null,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions an execution item to a new state.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="message">Optional message describing the transition.</param>
    /// <param name="actor">The user or system causing the transition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure with reason.</returns>
    Task<IGenericResult> TransitionState(
        Guid executionItemId,
        IExecutionStateType newState,
        string? message = null,
        string? actor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an event for an execution item.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="eventType">The type of event.</param>
    /// <param name="message">The event message.</param>
    /// <param name="data">Optional event data.</param>
    /// <param name="actor">The user or system causing the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<IExecutionEvent>> RecordEvent(
        Guid executionItemId,
        string eventType,
        string? message = null,
        IReadOnlyDictionary<string, object?>? data = null,
        string? actor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes an execution item with a result.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="success">Whether the execution was successful.</param>
    /// <param name="resultCode">The result code.</param>
    /// <param name="resultMessage">The result message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> Complete(
        Guid executionItemId,
        bool success,
        string? resultCode = null,
        string? resultMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution item by ID.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The execution item, or a failure result if not found.</returns>
    Task<IGenericResult<IExecutionItem>> GetItem(
        Guid executionItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events for an execution item.
    /// </summary>
    /// <param name="executionItemId">The execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of events.</returns>
    Task<IGenericResult<IReadOnlyList<IExecutionEvent>>> GetEvents(
        Guid executionItemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child execution items for a parent.
    /// </summary>
    /// <param name="parentId">The parent execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of child execution items.</returns>
    Task<IGenericResult<IReadOnlyList<IExecutionItem>>> GetChildren(
        Guid parentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all execution items with the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of execution items with matching correlation ID.</returns>
    Task<IGenericResult<IReadOnlyList<IExecutionItem>>> GetItems(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists execution items with pagination and optional filters.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="itemType">Optional filter by execution item type.</param>
    /// <param name="state">Optional filter by current state.</param>
    /// <param name="correlationId">Optional filter by correlation ID.</param>
    /// <param name="rootId">Optional filter by root execution: the run itself and everything under it (FDW-1690).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result of execution items.</returns>
    Task<IGenericResult<IPagedResponse<IExecutionItem>>> ListExecutions(
        int page = 1,
        int pageSize = 50,
        IExecutionItemType? itemType = null,
        IExecutionStateType? state = null,
        string? correlationId = null,
        Guid? rootId = null,
        CancellationToken cancellationToken = default);
}
