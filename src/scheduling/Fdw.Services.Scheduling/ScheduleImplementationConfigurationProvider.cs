using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Data;
using Fdw.Services.Scheduling.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Supplies the Schedule implementation's own configuration, records a fire on the schedule's current row (FDW-4598), counts the fires in a row that failed and reports the count to the thresholds that
/// judge the schedule, and pauses the schedule a threshold's rule fired on (FDW-6144).
/// </summary>
public sealed class ScheduleImplementationConfigurationProvider
    : ImplementationProviderBase<ScheduleImplementationConfiguration, IScheduleImplementationConfiguration>,
      IScheduleRunRecorder,
      ISchedulePauser
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly IScheduleFailureReporter _failures;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ScheduleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    /// <param name="failures">Reports a schedule's count of failed fires to the thresholds that judge it.</param>
    /// <param name="time">The clock a pause is stamped with.</param>
    public ScheduleImplementationConfigurationProvider(
        ILogger<ScheduleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName,
        IScheduleFailureReporter failures,
        TimeProvider time)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _gateways = gatewayProvider;
        _failures = failures;
        _time = time;
        _logger = logger ?? NullLogger<ScheduleImplementationConfigurationProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> RecordRun(
        IScheduleImplementationConfiguration schedule, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, string lastRunStatus, bool fireFailed, CancellationToken cancellationToken = default)
    {
        var scheduleId = schedule.Id;
        var found = _gateways.Get(DataStoreName);
        if (found.IsFailure || found.Value is not { } gateway)
        {
            return GenericResult.Failure(ScheduleRunRecordLog.StoreUnavailable(_logger, scheduleId, found.CurrentMessage ?? string.Empty));
        }

        // The count is the one value the thresholds judge, and it is this fire's to change: a fire that failed adds one to what the schedule holds, any other fire starts the count again.
        var consecutiveFailures = fireFailed ? schedule.ConsecutiveFailures + 1 : 0;

        var update = CmdBuilders.Update.In<ScheduleUpdateRecord>(ContainerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Where("Id", scheduleId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(new ScheduleUpdateRecord { LastRunTime = lastRunTime, NextRunTime = nextRunTime, LastRunStatus = lastRunStatus, ConsecutiveFailures = consecutiveFailures });
        var updated = await gateway.Execute<int>(update.Command, update.Target, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        // The cached reads of the container hold the old run fields until they are told otherwise.
        gateway.InvalidateCachedResults(update.Target);
        if (updated.Value != 1)
        {
            return GenericResult.Failure(ScheduleRunRecordLog.NotRecorded(_logger, scheduleId, updated.Value));
        }

        ScheduleRunRecordLog.Recorded(_logger, scheduleId, lastRunStatus, nextRunTime);
        return await _failures.Report(scheduleId, schedule.Name, schedule.TenantId, consecutiveFailures, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Pause(EscalationFiring firing, CancellationToken cancellationToken = default)
    {
        // Why the subject and nothing else: the firing's subject is the schedule the rule fired on, whichever threshold held the rule, so the schedule paused is exactly the one whose fires failed.
        if (ThresholdSubjectKinds.ByName(firing.Subject.Kind) is not ScheduleThresholdSubjectKind)
        {
            return GenericResult.Failure(ScheduleFailureLog.SubjectNotSchedule(_logger, firing.ThresholdName, firing.Subject.Kind, firing.Subject.Name));
        }

        var scheduleId = firing.Subject.SubjectId;
        var found = _gateways.Get(DataStoreName);
        if (found.IsFailure || found.Value is not { } gateway)
        {
            return GenericResult.Failure(ScheduleRunRecordLog.StoreUnavailable(_logger, scheduleId, found.CurrentMessage ?? string.Empty));
        }

        var update = CmdBuilders.Update.In<SchedulePauseRecord>(ContainerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Where("Id", scheduleId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(new SchedulePauseRecord { IsEnabled = false, PausedAt = _time.GetUtcNow(), PausedReason = firing.Reason });
        var updated = await gateway.Execute<int>(update.Command, update.Target, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return GenericResult.Failure(ScheduleFailureLog.PauseNotWritten(_logger, scheduleId, updated.CurrentMessage));
        }

        gateway.InvalidateCachedResults(update.Target);
        if (updated.Value != 1)
        {
            return GenericResult.Failure(ScheduleFailureLog.NotFoundToPause(_logger, scheduleId, $"{updated.Value} rows were changed"));
        }

        ScheduleFailureLog.SchedulePaused(_logger, scheduleId, firing.Reason);
        return GenericResult.Success();
    }
}
