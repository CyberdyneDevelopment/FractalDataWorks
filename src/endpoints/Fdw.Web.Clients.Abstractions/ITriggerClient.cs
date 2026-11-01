using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Web.Clients.Abstractions;

/// <summary>
/// Generic client interface for triggering operations and checking execution status.
/// Domain-specific clients implement this alongside their domain interface to provide
/// a composable, uniform trigger surface across all operation types.
/// </summary>
/// <typeparam name="TRequest">The trigger request type.</typeparam>
/// <typeparam name="TResponse">The trigger/status response type.</typeparam>
/// <remarks>
/// Type parameter constraints are relaxed to <c>class</c> rather than requiring
/// inheritance from <c>TriggerOperationRequest</c>/<c>TriggerOperationResponse</c>
/// because some domain client abstractions target <c>netstandard2.0</c> and cannot
/// reference the <c>net10.0</c> base types. The interface naming and documentation
/// communicate the intended contract.
/// </remarks>
public interface ITriggerClient<in TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Triggers an operation execution.
    /// </summary>
    /// <param name="request">The trigger request containing operation name and parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the trigger response with execution ID and initial state.</returns>
    Task<IGenericResult<TResponse>> Trigger(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of an execution by its ID.
    /// </summary>
    /// <param name="executionId">The execution identifier returned from <see cref="Trigger"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the current execution status.</returns>
    Task<IGenericResult<TResponse>> GetStatus(Guid executionId, CancellationToken cancellationToken = default);
}
