using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Scheduling.Abstractions;
using Fdw.Services.Execution.Abstractions.OptionTypes;
using Fdw.Services.Scheduling.Data;
using Fdw.Services.Scheduling.Execution;
using Fdw.Services.Scheduling.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Default implementation of <see cref="IFrameworkSchedulingService"/> using <see cref="IDataGateway"/>
/// for database-backed schedule persistence.
/// </summary>
/// <remarks>
/// <para>
/// This service provides CRUD operations for schedules stored in the database via the DataGateway pattern.
/// It maps between the <see cref="IGenericSchedule"/> domain model and the data records
/// (<see cref="ScheduleQueryRecord"/>, <see cref="ScheduleInsertRecord"/>, <see cref="ScheduleUpdateRecord"/>).
/// </para>
/// <para>
/// Tenant filtering is applied when an <see cref="ITenantContext"/> is available and has a current tenant.
/// System-wide schedules (TenantId IS NULL) are always included alongside tenant-specific schedules.
/// </para>
/// </remarks>
public sealed class DefaultSchedulingService : IFrameworkSchedulingService
{
    private readonly ILogger<DefaultSchedulingService> _logger;
    private readonly IDataGateway _dataGateway;
    private readonly ISchedulerImplementationConfiguration _configuration;
    private readonly ITenantContext? _tenantContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSchedulingService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="dataGateway">The data gateway for schedule data access.</param>
    /// <param name="configuration">The scheduler configuration containing DataStore, Path, and Container names.</param>
    /// <param name="tenantContext">Optional tenant context for multi-tenant schedule filtering.</param>
    public DefaultSchedulingService(
        ILogger<DefaultSchedulingService> logger,
        IDataGateway dataGateway,
        ISchedulerImplementationConfiguration configuration,
        ITenantContext? tenantContext = null)
    {
        _logger = logger ?? NullLogger<DefaultSchedulingService>.Instance;
        _dataGateway = dataGateway ?? throw new ArgumentNullException(nameof(dataGateway));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public IGenericScheduler Scheduler =>
        throw new NotSupportedException(
            "Direct scheduler access is not available in DefaultSchedulingService. " +
            "Use the service methods for schedule management.");

    /// <inheritdoc />
    public async Task<IGenericResult> CreateSchedule(
        IGenericSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Why: CreateSchedule is idempotent on Name — sched.Schedule has a unique index on
            // (Name) WHERE IsCurrent=1, so a duplicate insert raises SQL 2601. Treat an existing
            // active record as success rather than failing; callers that want strict
            // create-only semantics should call GetSchedule first.
            var existing = await GetScheduleRecord(schedule.ScheduleName, cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                SchedulingLog.ScheduleCreated(_logger, schedule.ScheduleName, schedule.ProcessId);
                return GenericResult.Success();
            }

            var record = new ScheduleInsertRecord
            {
                // Why: uuid v7 for time-orderable persistence. NO fallback — the DB column is
                // uniqueidentifier NOT NULL with no default; missing Id surfaces as a SQL 515.
                Id = Guid.CreateVersion7(),
                Name = schedule.ScheduleName,
                PipelineName = schedule.ProcessId,
                ServiceOptionType = "Cron",
                CronExpression = schedule.CronExpression,
                TimeZoneId = schedule.TimeZoneId,
                IsEnabled = schedule.IsActive,
                TenantId = _tenantContext?.TenantId
            };

            var command = Insert.Into<ScheduleInsertRecord>(_configuration.ScheduleContainerName)
                .DataStore(_configuration.DataStoreName)
                .Path(_configuration.PathName)
                .Value(record);

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult.Failure(
                    SchedulingLog.CreationFailed(_logger, schedule.ScheduleName, "DataGateway insert failed"));
            }

            SchedulingLog.ScheduleCreated(_logger, schedule.ScheduleName, schedule.ProcessId);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, schedule.ScheduleName));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> UpdateSchedule(
        IGenericSchedule schedule,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var record = new ScheduleUpdateRecord
            {
                LastRunTime = null,
                NextRunTime = schedule.NextExecution.HasValue
                    ? new DateTimeOffset(schedule.NextExecution.Value, TimeSpan.Zero)
                    : null
            };

            var command = Update.In<ScheduleUpdateRecord>(_configuration.ScheduleContainerName)
                .DataStore(_configuration.DataStoreName)
                .Path(_configuration.PathName)
                .Where(nameof(ScheduleQueryRecord.Name), schedule.ScheduleName)
                .Value(record);

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult.Failure(
                    SchedulingLog.SchedulingFailed(_logger, schedule.ScheduleName, "DataGateway update failed"));
            }

            if (result.Value == 0)
            {
                return GenericResult.Failure(
                    SchedulingLog.ScheduleNotFound(_logger, schedule.ScheduleName));
            }

            SchedulingLog.ScheduleUpdated(_logger, schedule.ScheduleName);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, schedule.ScheduleName));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeleteSchedule(
        string scheduleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = Delete.From(_configuration.ScheduleContainerName)
                .DataStore(_configuration.DataStoreName)
                .Path(_configuration.PathName)
                .Where(nameof(ScheduleQueryRecord.Name), scheduleId)
                .Build();

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult.Failure(
                    SchedulingLog.SchedulingFailed(_logger, scheduleId, "DataGateway delete failed"));
            }

            if (result.Value == 0)
            {
                return GenericResult.Failure(
                    SchedulingLog.ScheduleNotFound(_logger, scheduleId));
            }

            SchedulingLog.ScheduleDeleted(_logger, scheduleId);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, scheduleId));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> PauseSchedule(
        string scheduleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var getResult = await GetScheduleRecord(scheduleId, cancellationToken).ConfigureAwait(false);
            if (getResult == null)
            {
                return GenericResult.Failure(
                    SchedulingLog.ScheduleNotFound(_logger, scheduleId));
            }

            // Update IsEnabled to false via a raw update on the IsEnabled column
            var command = Update.In<ScheduleInsertRecord>(_configuration.ScheduleContainerName)
                .DataStore(_configuration.DataStoreName)
                .Path(_configuration.PathName)
                .Where(nameof(ScheduleQueryRecord.Name), scheduleId)
                .Value(new ScheduleInsertRecord
                {
                    Name = getResult.Name,
                    PipelineName = getResult.PipelineName,
                    ServiceOptionType = getResult.ServiceOptionType,
                    CronExpression = getResult.CronExpression,
                    IntervalSeconds = getResult.IntervalSeconds,
                    TimeZoneId = getResult.TimeZoneId,
                    IsEnabled = false,
                    TenantId = getResult.TenantId
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult.Failure(
                    SchedulingLog.SchedulingFailed(_logger, scheduleId, "DataGateway pause update failed"));
            }

            SchedulingLog.SchedulePaused(_logger, scheduleId);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, scheduleId));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> ResumeSchedule(
        string scheduleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var getResult = await GetScheduleRecord(scheduleId, cancellationToken).ConfigureAwait(false);
            if (getResult == null)
            {
                return GenericResult.Failure(
                    SchedulingLog.ScheduleNotFound(_logger, scheduleId));
            }

            // Update IsEnabled to true
            var command = Update.In<ScheduleInsertRecord>(_configuration.ScheduleContainerName)
                .DataStore(_configuration.DataStoreName)
                .Path(_configuration.PathName)
                .Where(nameof(ScheduleQueryRecord.Name), scheduleId)
                .Value(new ScheduleInsertRecord
                {
                    Name = getResult.Name,
                    PipelineName = getResult.PipelineName,
                    ServiceOptionType = getResult.ServiceOptionType,
                    CronExpression = getResult.CronExpression,
                    IntervalSeconds = getResult.IntervalSeconds,
                    TimeZoneId = getResult.TimeZoneId,
                    IsEnabled = true,
                    TenantId = getResult.TenantId
                });

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult.Failure(
                    SchedulingLog.SchedulingFailed(_logger, scheduleId, "DataGateway resume update failed"));
            }

            SchedulingLog.ScheduleResumed(_logger, scheduleId);

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, scheduleId));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult> TriggerSchedule(
        string scheduleId,
        CancellationToken cancellationToken = default)
    {
        var record = await GetScheduleRecord(scheduleId, cancellationToken).ConfigureAwait(false);
        if (record == null)
        {
            return GenericResult.Failure(
                SchedulingLog.ScheduleNotFound(_logger, scheduleId));
        }

        var now = DateTimeOffset.UtcNow;
        var updateRecord = new ScheduleUpdateRecord
        {
            LastRunTime = now,
            NextRunTime = record.NextRunTime
        };

        var command = Update.In<ScheduleUpdateRecord>(_configuration.ScheduleContainerName)
            .DataStore(_configuration.DataStoreName)
            .Path(_configuration.PathName)
            .Where(nameof(ScheduleQueryRecord.Name), scheduleId)
            .Value(updateRecord);

        var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return GenericResult.Failure(
                SchedulingLog.SchedulingFailed(_logger, scheduleId, "DataGateway trigger update failed"));
        }

        return GenericResult.Success();
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IGenericSchedule?>> GetSchedule(
        string scheduleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var record = await GetScheduleRecord(scheduleId, cancellationToken).ConfigureAwait(false);

            if (record == null)
            {
                return GenericResult<IGenericSchedule?>.Failure(
                    SchedulingLog.ScheduleNotFound(_logger, scheduleId));
            }

            var schedule = MapToSchedule(record);

            return GenericResult<IGenericSchedule?>.Success(schedule);
        }
        catch (Exception ex)
        {
            return GenericResult<IGenericSchedule?>.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, scheduleId));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<IGenericSchedule>>> GetSchedules(
        bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Query.From<ScheduleQueryRecord>(
                _configuration.DataStoreName,
                _configuration.PathName,
                _configuration.ScheduleContainerName);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsEnabled).Equal(true);
            }

            // Filter by tenant if tenant context is available
            if (_tenantContext?.HasTenant == true && _tenantContext.TenantId.HasValue)
            {
                query = query.BeginOrGroup()
                    .Where(s => s.TenantId).Equal(_tenantContext.TenantId.Value)
                    .Where(s => s.TenantId).IsNull()
                    .EndGroup();
            }

            var command = query.OrderBy(nameof(ScheduleQueryRecord.Name)).Build();

            var result = await _dataGateway.Execute<IEnumerable<ScheduleQueryRecord>>(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult<IReadOnlyCollection<IGenericSchedule>>.Failure(
                    SchedulingLog.SchedulingFailed(_logger, "GetSchedules", "DataGateway query failed"));
            }

            var schedules = result.Value!
                .Select(MapToSchedule)
                .ToList();

            SchedulingLog.SchedulesLoaded(_logger, schedules.Count);

            IReadOnlyCollection<IGenericSchedule> readOnlySchedules = schedules;

            return GenericResult<IReadOnlyCollection<IGenericSchedule>>.Success(readOnlySchedules);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyCollection<IGenericSchedule>>.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, "GetSchedules"));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>> GetScheduleHistory(
        string scheduleId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = Query.From<ScheduleExecutionRecord>(
                _configuration.DataStoreName,
                _configuration.PathName,
                _configuration.ScheduleContainerName + "History")
                .Where("ScheduleName", scheduleId);

            if (startDate.HasValue)
            {
                query = query.Where(r => r.TriggeredAt).GreaterThanOrEqual(new DateTimeOffset(startDate.Value, TimeSpan.Zero));
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.TriggeredAt).LessThanOrEqual(new DateTimeOffset(endDate.Value, TimeSpan.Zero));
            }

            var command = query.OrderBy("TriggeredAt").Build();
            var result = await _dataGateway.Execute<IEnumerable<ScheduleExecutionRecord>>(command, cancellationToken)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>.Failure(
                    SchedulingLog.SchedulingFailed(_logger, scheduleId, "DataGateway history query failed"));
            }

            IReadOnlyCollection<IGenericScheduleExecutionHistory> history = result.Value!
                .Select(MapToHistory)
                .ToList();

            return GenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>.Success(history);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyCollection<IGenericScheduleExecutionHistory>>.Failure(
                SchedulingLog.GetSchedulingException(_logger, ex, scheduleId));
        }
    }

    #region IGenericService Implementation

    /// <inheritdoc />
    public string Id => _configuration.Id.ToString("N");

    /// <inheritdoc />
    public string Name => _configuration.Name;

    /// <inheritdoc />
    public string ServiceType => _configuration.ServiceType;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        // Why: Target-typed gateway refactor stripped addressing from IDataCommand. A bare IDataCommand
        // no longer carries DataStore/Path/Container, so gateway execution requires a DataStoreTarget.
        // This IGenericService.Execute(IGenericCommand) contract cannot be satisfied without a target;
        // callers must use the typed service methods instead.
        return Task.FromResult(
            GenericResult<T>.Failure(
                SchedulingLog.SchedulingFailed(_logger, "Execute", "IGenericCommand execution is not supported; use typed service methods")));
    }

    /// <inheritdoc />
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default)
    {
        // Why: See Execute<T> — target-typed gateway refactor removed addressing from IDataCommand.
        return Task.FromResult(
            (IGenericResult)GenericResult.Failure(
                SchedulingLog.SchedulingFailed(_logger, "Execute", "IGenericCommand execution is not supported; use typed service methods")));
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Retrieves a single schedule record by name, applying tenant filtering.
    /// </summary>
    private async Task<ScheduleQueryRecord?> GetScheduleRecord(
        string scheduleName,
        CancellationToken cancellationToken)
    {
        var query = Query.From<ScheduleQueryRecord>(
                _configuration.DataStoreName,
                _configuration.PathName,
                _configuration.ScheduleContainerName)
            .Where(nameof(ScheduleQueryRecord.Name), scheduleName);

        // Filter by tenant if tenant context is available
        if (_tenantContext?.HasTenant == true && _tenantContext.TenantId.HasValue)
        {
            query = query.BeginOrGroup()
                .Where(s => s.TenantId).Equal(_tenantContext.TenantId.Value)
                .Where(s => s.TenantId).IsNull()
                .EndGroup();
        }

        var command = query.Build();

        var result = await _dataGateway.Execute<IEnumerable<ScheduleQueryRecord>>(command, cancellationToken)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return null;
        }

        return result.Value?.FirstOrDefault();
    }

    /// <summary>
    /// Maps a <see cref="ScheduleExecutionRecord"/> to an <see cref="IGenericScheduleExecutionHistory"/> implementation.
    /// </summary>
    private static ExecutionHistoryRecord MapToHistory(ScheduleExecutionRecord record)
    {
        var status = ProcessStates.ByName(record.Status);
        return new ExecutionHistoryRecord(
            executionId: record.Id.ToString("N"),
            scheduleId: record.ScheduleName,
            triggeredAt: record.TriggeredAt.UtcDateTime,
            startedAt: record.StartedAt?.UtcDateTime,
            completedAt: record.CompletedAt?.UtcDateTime,
            duration: record.DurationMs.HasValue ? TimeSpan.FromMilliseconds(record.DurationMs.Value) : null,
            wasTriggeredManually: string.Equals(record.TriggerType, "Manual", StringComparison.Ordinal),
            status: status,
            errorMessage: record.ErrorMessage);
    }

    /// <summary>
    /// Maps a <see cref="ScheduleQueryRecord"/> to an <see cref="IGenericSchedule"/> implementation.
    /// </summary>
    private static ScheduleRecord MapToSchedule(ScheduleQueryRecord record)
    {
        return new ScheduleRecord(
            scheduleId: record.Name,
            scheduleName: record.Name,
            processId: record.PipelineName,
            cronExpression: record.CronExpression ?? string.Empty,
            isActive: record.IsEnabled,
            timeZoneId: record.TimeZoneId,
            nextExecution: record.NextRunTime?.UtcDateTime);
    }

    #endregion

    #region Inner Types

    /// <summary>
    /// Lightweight read-only implementation of <see cref="IGenericSchedule"/> for query results.
    /// </summary>
    private sealed class ScheduleRecord : IGenericSchedule
    {
        public ScheduleRecord(
            string scheduleId,
            string scheduleName,
            string processId,
            string cronExpression,
            bool isActive,
            string timeZoneId,
            DateTime? nextExecution)
        {
            ScheduleId = scheduleId;
            ScheduleName = scheduleName;
            ProcessId = processId;
            CronExpression = cronExpression;
            IsActive = isActive;
            TimeZoneId = timeZoneId;
            NextExecution = nextExecution;
        }

        public string ScheduleId { get; }
        public string ScheduleName { get; }
        public string ProcessId { get; }
        public string CronExpression { get; }
        public DateTime? NextExecution { get; }
        public bool IsActive { get; }
        public string TimeZoneId { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }

    /// <summary>
    /// Lightweight read-only implementation of <see cref="IGenericScheduleExecutionHistory"/> for query results.
    /// </summary>
    private sealed class ExecutionHistoryRecord : IGenericScheduleExecutionHistory
    {
        public ExecutionHistoryRecord(
            string executionId,
            string scheduleId,
            DateTime triggeredAt,
            DateTime? startedAt,
            DateTime? completedAt,
            TimeSpan? duration,
            bool wasTriggeredManually,
            IProcessState status,
            string? errorMessage)
        {
            ExecutionId = executionId;
            ScheduleId = scheduleId;
            TriggeredAt = triggeredAt;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            Duration = duration;
            WasTriggeredManually = wasTriggeredManually;
            Status = status;
            ErrorMessage = errorMessage;
        }

        public string ExecutionId { get; }
        public string ScheduleId { get; }
        public DateTime TriggeredAt { get; }
        public DateTime? StartedAt { get; }
        public DateTime? CompletedAt { get; }
        public TimeSpan? Duration { get; }
        public bool WasTriggeredManually { get; }
        public IProcessState Status { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyDictionary<string, object>? Metadata => null;
    }

    #endregion
}
