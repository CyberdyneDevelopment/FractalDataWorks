using System;
using System.Threading;
using System.Threading.Tasks;
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

/// <summary>Supplies the Schedule implementation's own configuration, and records a fire on the schedule's current row (FDW-4598).</summary>
public sealed class ScheduleImplementationConfigurationProvider
    : ImplementationProviderBase<ScheduleImplementationConfiguration, IScheduleImplementationConfiguration>,
      IScheduleRunRecorder
{
    private readonly IConfigurationGatewayProvider _gateways;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ScheduleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    /// <param name="pathName">The schema its rows live in -- passed by its registration.</param>
    /// <param name="containerName">The container its rows live in -- passed by its registration.</param>
    public ScheduleImplementationConfigurationProvider(
        ILogger<ScheduleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName,
        string containerName)
        : base(logger, gatewayProvider, dataStoreName, pathName, containerName)
    {
        _gateways = gatewayProvider;
        _logger = logger ?? NullLogger<ScheduleImplementationConfigurationProvider>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> RecordRun(
        Guid scheduleId, DateTimeOffset lastRunTime, DateTimeOffset? nextRunTime, string lastRunStatus, CancellationToken cancellationToken = default)
    {
        var found = _gateways.Get(DataStoreName);
        if (found.IsFailure || found.Value is not { } gateway)
        {
            return GenericResult.Failure(ScheduleRunRecordLog.StoreUnavailable(_logger, scheduleId, found.CurrentMessage ?? string.Empty));
        }

        var update = CmdBuilders.Update.In<ScheduleUpdateRecord>(ContainerName)
            .DataStore(DataStoreName)
            .Path(PathName)
            .Where("Id", scheduleId)
            .Where("IsCurrent", true)
            .Where("IsDeleted", false)
            .Value(new ScheduleUpdateRecord { LastRunTime = lastRunTime, NextRunTime = nextRunTime, LastRunStatus = lastRunStatus });
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
        return GenericResult.Success();
    }
}
