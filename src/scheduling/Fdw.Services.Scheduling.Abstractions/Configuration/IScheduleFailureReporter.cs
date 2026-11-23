using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.Configuration;

/// <summary>
/// Reports how many fires of a schedule in a row failed to the thresholds that judge it (FDW-6144): the thresholds attached to the schedule and the tenant-wide thresholds of the tenant, each judged
/// against this schedule. The scheduler's recorder calls it after a fire's outcome is written.
/// </summary>
public interface IScheduleFailureReporter
{
    /// <summary>Judges the schedule's thresholds against its count of failed fires.</summary>
    /// <param name="scheduleId">The schedule's durable Id.</param>
    /// <param name="scheduleName">The schedule's name.</param>
    /// <param name="tenantId">The schedule's tenant; none is a refusal, because a threshold belongs to a tenant.</param>
    /// <param name="consecutiveFailures">How many fires in a row failed, counting the one just recorded.</param>
    /// <param name="cancellationToken">Cancels the calls.</param>
    /// <returns>Success when the thresholds were judged (whether or not any fired); a failure when they could not be read or judged.</returns>
    Task<IGenericResult> Report(Guid scheduleId, string scheduleName, Guid? tenantId, long consecutiveFailures, CancellationToken cancellationToken = default);
}
