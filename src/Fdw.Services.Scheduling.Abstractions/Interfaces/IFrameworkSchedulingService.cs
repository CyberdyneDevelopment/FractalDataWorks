using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;

namespace Fdw.Services.Scheduling.Abstractions;

/// <summary>
/// Main service interface for scheduling operations that applications use to interact with the scheduling system.
/// </summary>
/// <remarks>
/// <para>
/// The IFractalSchedulingService provides a high-level API for managing schedules and controlling their execution.
/// It serves as the primary entry point for applications that need to create, manage, and monitor scheduled processes.
/// </para>
/// <para>
/// This service abstracts the complexity of the underlying scheduler implementation while providing comprehensive
/// lifecycle management, execution control, and query capabilities for schedules.
/// </para>
/// <para>
/// The service follows the standard Fdw service patterns by inheriting from IFractalService and returning
/// IGenericResult types for consistent error handling and result management across the framework.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a new schedule
/// var schedule = new Schedule
/// {
///     ScheduleId = "daily-report",
///     ScheduleName = "Daily Report Generation",
///     ProcessId = "report-generator",
///     CronExpression = "0 9 * * *", // Daily at 9 AM
///     TimeZoneId = "America/New_York"
/// };
///
/// var result = await schedulingService.CreateScheduleAsync(schedule, cancellationToken);
/// if (result.Error)
/// {
///     // Handle error
///     return;
/// }
///
/// // Pause the schedule temporarily
/// await schedulingService.PauseScheduleAsync("daily-report", cancellationToken);
///
/// // Resume the schedule
/// await schedulingService.ResumeScheduleAsync("daily-report", cancellationToken);
///
/// // Trigger immediate execution
/// await schedulingService.TriggerScheduleAsync("daily-report", cancellationToken);
///
/// // Get schedule information
/// var scheduleResult = await schedulingService.GetScheduleAsync("daily-report", cancellationToken);
/// if (!scheduleResult.Error &amp;&amp; scheduleResult.Value != null)
/// {
///     Console.WriteLine($"Next execution: {scheduleResult.Value.NextExecution}");
/// }
/// </code>
/// </example>
public interface IFrameworkSchedulingService : IServiceOption
{
    /// <summary>
    /// Gets the underlying scheduler instance used by this service.
    /// </summary>
    /// <value>The scheduler responsible for managing schedule timing and execution triggers.</value>
    /// <remarks>
    /// This property provides access to the core scheduler for advanced scenarios where direct
    /// scheduler interaction is needed. Most applications should use the service methods instead
    /// of accessing the scheduler directly.
    /// </remarks>
    IGenericScheduler Scheduler { get; }

    /// <summary>
    /// Creates a new schedule and registers it with the scheduling system.
    /// </summary>
    /// <param name="schedule">The schedule definition to create and register.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the schedule creation operation.</returns>
    /// <remarks>
    /// <para>
    /// This method validates the schedule configuration, registers it with the underlying scheduler,
    /// and begins monitoring for trigger conditions. The schedule will become active immediately
    /// unless specified otherwise in the schedule configuration.
    /// </para>
    /// <para>
    /// The schedule must have a valid cron expression and reference an existing process that
    /// can be executed by the system's execution handler.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedule"/> is null.</exception>
    Task<IGenericResult> CreateSchedule(IGenericSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing schedule with new configuration.
    /// </summary>
    /// <param name="schedule">The updated schedule definition with the same ScheduleId.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the schedule update operation.</returns>
    /// <remarks>
    /// <para>
    /// This method allows modification of an existing schedule's properties including its cron expression,
    /// metadata, and other configuration settings. The schedule is identified by its ScheduleId property.
    /// </para>
    /// <para>
    /// If the cron expression is changed, the next execution time will be recalculated immediately.
    /// Any currently executing instance triggered by this schedule will continue to run with the
    /// old configuration.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedule"/> is null.</exception>
    Task<IGenericResult> UpdateSchedule(IGenericSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schedule and removes it from the scheduling system.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to delete.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the schedule deletion operation.</returns>
    /// <remarks>
    /// <para>
    /// This method permanently removes a schedule from the scheduling system. Once deleted,
    /// the schedule will no longer trigger automatic executions and cannot be recovered
    /// without recreating it.
    /// </para>
    /// <para>
    /// Any currently executing instance triggered by this schedule will continue to run,
    /// but no new executions will be triggered.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleId"/> is null or empty.</exception>
    Task<IGenericResult> DeleteSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses a schedule, preventing it from triggering new executions.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to pause.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the pause operation.</returns>
    /// <remarks>
    /// <para>
    /// A paused schedule remains in the system but will not trigger new executions until
    /// it is resumed. The schedule's next execution time continues to be calculated
    /// but is not acted upon.
    /// </para>
    /// <para>
    /// This is useful for temporarily disabling a schedule without losing its configuration
    /// or execution history.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleId"/> is null or empty.</exception>
    Task<IGenericResult> PauseSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a previously paused schedule, allowing it to trigger executions again.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to resume.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the resume operation.</returns>
    /// <remarks>
    /// <para>
    /// A resumed schedule will immediately recalculate its next execution time based on
    /// its cron expression and the current time, then begin triggering executions normally.
    /// </para>
    /// <para>
    /// This method has no effect if the schedule is already active.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleId"/> is null or empty.</exception>
    Task<IGenericResult> ResumeSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers immediate execution of a scheduled process, bypassing normal schedule timing.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to trigger immediately.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the result of the trigger operation.</returns>
    /// <remarks>
    /// <para>
    /// This method allows manual execution of a scheduled process without waiting for its
    /// next scheduled time. The schedule itself remains unchanged and will continue to
    /// trigger at its normal intervals.
    /// </para>
    /// <para>
    /// The execution is handled through the same execution pipeline as scheduled triggers,
    /// ensuring consistent behavior and monitoring.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleId"/> is null or empty.</exception>
    Task<IGenericResult> TriggerSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific schedule by its identifier.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the requested schedule, or null if not found.</returns>
    /// <remarks>
    /// <para>
    /// This method allows lookup of individual schedules for inspection, monitoring,
    /// or management purposes. The returned schedule includes current state information
    /// such as next execution time and active status.
    /// </para>
    /// <para>
    /// Returns a successful result with null value if no schedule with the specified
    /// identifier exists.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleId"/> is null or empty.</exception>
    Task<IGenericResult<IGenericSchedule?>> GetSchedule(string scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all schedules currently managed by the scheduling system.
    /// </summary>
    /// <param name="includeInactive">Whether to include paused/inactive schedules in the results. Defaults to true.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing a collection of all schedules matching the filter criteria.</returns>
    /// <remarks>
    /// <para>
    /// This method provides visibility into all schedules managed by the scheduling system.
    /// The results can be filtered to show only active schedules or include all schedules
    /// regardless of their state.
    /// </para>
    /// <para>
    /// The returned collection is read-only and represents a snapshot of the schedules
    /// at the time of the query.
    /// </para>
    /// </remarks>
    Task<IGenericResult<IReadOnlyCollection<IGenericSchedule>>> GetSchedules(bool includeInactive = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the execution history for a specific schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to get history for.</param>
    /// <param name="startDate">The earliest date to include in the history. If null, includes all available history.</param>
    /// <param name="endDate">The latest date to include in the history. If null, includes up to the current time.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the execution history for the specified schedule.</returns>
    /// <remarks>
    /// <para>
    /// This method provides detailed information about past executions of a schedule,
    /// including execution times, results, and any error information. This is useful
    /// for monitoring, debugging, and auditing purposes.
    /// </para>
    /// <para>
    /// The history can be filtered by date range to limit the amount of data returned.
    /// If no date filters are provided, all available history is returned.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="scheduleId"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="startDate"/> is greater than <paramref name="endDate"/>.</exception>
    Task<IGenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>> GetScheduleHistory(
        string scheduleId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
