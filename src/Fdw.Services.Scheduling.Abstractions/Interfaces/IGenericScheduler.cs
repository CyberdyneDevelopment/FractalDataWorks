using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using IGenericServiceBase = Fdw.Abstractions.IGenericService;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Core scheduling engine interface that manages WHEN to trigger process execution.
/// </summary>
/// <remarks>
/// This interface represents the scheduling engine responsible for managing schedule registration,
/// timing, and triggering. It bridges scheduling (WHEN) to execution (HOW) by calling the
/// ExecutionHandler when schedules trigger. The scheduler is concerned only with timing and
/// schedule management, not with the actual execution of processes.
/// </remarks>
public interface IGenericScheduler : IDisposable, IGenericServiceBase
{
    /// <summary>
    /// Gets or sets the execution handler that processes triggered schedules.
    /// </summary>
    /// <value>The handler responsible for executing processes when schedules trigger.</value>
    /// <remarks>
    /// This property provides the bridge between scheduling and execution. When a schedule
    /// triggers, the scheduler calls the ExecutionHandler to perform the actual work.
    /// </remarks>
    IGenericScheduledExecutionHandler ExecutionHandler { get; set; }

    /// <summary>
    /// Registers a new schedule with the scheduling engine.
    /// </summary>
    /// <param name="schedule">The schedule to register.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the schedule registration.</returns>
    /// <remarks>
    /// Once registered, the scheduler will monitor the schedule and trigger execution
    /// according to its cron expression. The schedule must have a valid cron expression
    /// and reference a process that the ExecutionHandler can handle.
    /// </remarks>
    Task<IGenericResult> Schedule(IGenericSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a schedule from the scheduling engine.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to remove.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the schedule removal.</returns>
    /// <remarks>
    /// Once unscheduled, the schedule will no longer trigger automatic executions.
    /// Any currently executing instance triggered by this schedule will continue to run.
    /// </remarks>
    Task<IGenericResult> Unschedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Temporarily pauses a schedule, preventing it from triggering.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to pause.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the pause operation.</returns>
    /// <remarks>
    /// A paused schedule remains registered but will not trigger execution until resumed.
    /// The schedule's next execution time continues to be calculated but is not acted upon.
    /// </remarks>
    Task<IGenericResult> PauseSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously paused schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to resume.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the resume operation.</returns>
    /// <remarks>
    /// A resumed schedule will immediately recalculate its next execution time and
    /// begin triggering according to its cron expression.
    /// </remarks>
    Task<IGenericResult> ResumeSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all schedules currently registered with the scheduler.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive/paused schedules in the results.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing a collection of all registered schedules.</returns>
    /// <remarks>
    /// This method provides visibility into all schedules managed by the scheduler.
    /// The results can be filtered to show only active schedules or include all schedules
    /// regardless of their state.
    /// </remarks>
    Task<IGenericResult<IReadOnlyCollection<IGenericSchedule>>> GetSchedules(bool includeInactive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific schedule by its identifier.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the requested schedule, or null if not found.</returns>
    /// <remarks>
    /// This method allows lookup of individual schedules for inspection or management.
    /// Returns null if no schedule with the specified identifier exists.
    /// </remarks>
    Task<IGenericResult<IGenericSchedule?>> GetSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Immediately triggers a scheduled process, bypassing the normal schedule timing.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to trigger immediately.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the immediate trigger operation.</returns>
    /// <remarks>
    /// This method allows manual execution of a scheduled process without waiting for
    /// its next scheduled time. The schedule itself remains unchanged and will continue
    /// to trigger at its normal intervals. The execution is handled by the ExecutionHandler.
    /// </remarks>
    Task<IGenericResult> TriggerNow(string scheduleId, CancellationToken cancellationToken = default);
}