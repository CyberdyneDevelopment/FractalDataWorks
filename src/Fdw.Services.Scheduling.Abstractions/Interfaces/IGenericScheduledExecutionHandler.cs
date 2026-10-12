using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Execution.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Handles the execution of processes when schedules trigger.
/// This interface bridges the gap between scheduling (WHEN) and execution (HOW).
/// </summary>
/// <remarks>
/// The scheduler calls this handler when a schedule triggers, passing the process identifier
/// and any relevant context. The handler is responsible for locating and executing the
/// appropriate process implementation.
/// </remarks>
public interface IGenericScheduledExecutionHandler
{
    /// <summary>
    /// Executes a process that was triggered by a schedule.
    /// </summary>
    /// <param name="processId">The identifier of the process to execute.</param>
    /// <param name="scheduleId">The identifier of the schedule that triggered this execution.</param>
    /// <param name="scheduledTime">The time this execution was scheduled for.</param>
    /// <param name="actualTime">The actual time this execution started.</param>
    /// <param name="metadata">Optional metadata from the schedule or execution context.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the process execution.</returns>
    Task<IGenericResult<IProcessResult>> ExecuteScheduledProcess(
        string processId,
        string scheduleId,
        DateTime scheduledTime,
        DateTime actualTime,
        IReadOnlyDictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a process with the specified identifier can be handled by this execution handler.
    /// </summary>
    /// <param name="processId">The identifier of the process to check.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing whether the process can be handled.</returns>
    Task<IGenericResult<bool>> CanHandleProcess(string processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a process without executing it.
    /// </summary>
    /// <param name="processId">The identifier of the process to get information about.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the process information, or null if the process is not found.</returns>
    Task<IGenericResult<IProcess?>> GetProcessInfo(string processId, CancellationToken cancellationToken = default);
}