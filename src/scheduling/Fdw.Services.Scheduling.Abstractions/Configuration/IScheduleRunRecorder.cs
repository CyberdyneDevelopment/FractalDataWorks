using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// Records what a scheduled fire did to its schedule: when it last ran, how it ended and when it runs next (FDW-4598).
/// </summary>
/// <remarks>
/// The three fields live on the schedule's implementation row, so the write goes through the provider that owns that container and not through whatever container a job
/// was told to write. It changes those columns on the current row and writes no new version: a schedule's version and its failure counts key on the row's ModifyDate.
/// </remarks>
public interface IScheduleRunRecorder
{
    /// <summary>Records a fire on the current row of a schedule.</summary>
    /// <param name="scheduleId">The schedule's durable Id.</param>
    /// <param name="lastRunTime">When the fire ran.</param>
    /// <param name="nextRunTime">When the schedule fires next; null when it does not.</param>
    /// <param name="lastRunStatus">How the fire ended.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a coded failure when the schedule has no current row, or the write fails.</returns>
    Task<IGenericResult> RecordRun(
        Guid scheduleId, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, string lastRunStatus, CancellationToken cancellationToken = default);
}
