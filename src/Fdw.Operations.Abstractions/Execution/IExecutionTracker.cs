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
public interface IExecutionTracker
{
    /// <summary>
    /// Creates a new execution item.
    /// </summary>
    /// <param name="itemType">The type of execution item.</param>
    /// <param name="name">The name of the execution item.</param>
    /// <param name="domainConfigurationId">The parent execution item ID, if any.</param>
    /// <param name="correlationId">The correlation ID for distributed tracing.</param>
    /// <param name="triggerSource">The source that triggered this execution.</param>
    /// <param name="parameters">The execution parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created execution item.</returns>
    Task<IGenericResult<IExecutionItem>> CreateItem(
        IExecutionItemType itemType,
        string name,
        Guid? domainConfigurationId = null,
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
    /// <param name="domainConfigurationId">The parent execution item ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of child execution items.</returns>
    Task<IGenericResult<IReadOnlyList<IExecutionItem>>> GetChildren(
        Guid domainConfigurationId,
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result of execution items.</returns>
    Task<IGenericResult<IPagedResponse<IExecutionItem>>> ListExecutions(
        int page = 1,
        int pageSize = 50,
        IExecutionItemType? itemType = null,
        IExecutionStateType? state = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
