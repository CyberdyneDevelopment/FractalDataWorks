using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Interface for scheduling service implementations.
/// Provides task scheduling, execution monitoring, and lifecycle management capabilities.
/// </summary>
public interface ISchedulingService : IGenericService
{
    /// <summary>
    /// Schedules a task for execution using the provided task configuration.
    /// </summary>
    /// <param name="task">The scheduled task configuration.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous scheduling operation.</returns>
    Task<IGenericResult<string>> ScheduleTask(IScheduledTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a previously scheduled task.
    /// </summary>
    /// <param name="taskId">The identifier of the task to cancel.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous cancellation operation.</returns>
    Task<IGenericResult> CancelTask(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a scheduled task.
    /// </summary>
    /// <param name="taskId">The identifier of the task to query.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that represents the asynchronous status query operation.</returns>
    Task<IGenericResult<string>> GetTaskStatus(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a scheduling command and returns a typed result.
    /// Integrates with the command pattern for unified service execution.
    /// </summary>
    /// <typeparam name="TResult">The type of result expected from the command execution.</typeparam>
    /// <param name="command">The scheduling command to execute.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the command execution to complete.</param>
    /// <returns>A task that represents the asynchronous command execution operation.</returns>
    Task<IGenericResult<TResult>> Execute<TResult>(ISchedulingCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a scheduling command without returning a specific result type.
    /// Integrates with the command pattern for unified service execution.
    /// </summary>
    /// <param name="command">The scheduling command to execute.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the command execution to complete.</param>
    /// <returns>A task that represents the asynchronous command execution operation.</returns>
    Task<IGenericResult> Execute(ISchedulingCommand command, CancellationToken cancellationToken = default);
}