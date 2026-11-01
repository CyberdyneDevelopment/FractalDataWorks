using System;
using Fdw.Services.Resiliency.Abstractions;
using Fdw.Services.Scheduling.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Resiliency.PrimaryBackup;

/// <summary>
/// Extended execution context for the PrimaryBackup strategy.
/// Provides access to the <see cref="IScheduleClient"/> for triggering the refresh schedule
/// and the <see cref="ILogger"/> for strategy-specific logging.
/// </summary>
/// <remarks>
/// Why extended context: TypeOptions must be DI-free (singleton prototypes). Services
/// needed for side effects are accessed via the context, which is created by the executor
/// and populated with injected services before calling Execute.
/// </remarks>
public interface IPrimaryBackupResiliencyContext : IResiliencyExecutionContext
{
    /// <summary>
    /// Gets the schedule client for triggering the refresh pipeline.
    /// </summary>
    IScheduleClient ScheduleClient { get; }

    /// <summary>
    /// Gets the logger for strategy-specific log messages.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Signals the execution context that the backup data set should now be active.
    /// The run delegate reads this flag to route data access to the backup source.
    /// </summary>
    /// <param name="backupDataSetId">The backup data set identifier to activate.</param>
    void ActivateBackup(Guid backupDataSetId);

    /// <summary>
    /// Gets whether the backup source is currently active for this execution.
    /// </summary>
    bool IsBackupActive { get; }

    /// <summary>
    /// Gets the active backup data set identifier, or <c>null</c> if backup is not active.
    /// </summary>
    Guid? ActiveBackupDataSetId { get; }
}
