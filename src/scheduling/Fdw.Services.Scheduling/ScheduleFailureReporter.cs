using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Scheduling.Abstractions.Configuration;
using Fdw.Services.Scheduling.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Scheduling;

/// <summary>
/// Judges a schedule's failed fires against its thresholds (FDW-6144). It reads the thresholds attached to the schedule and the tenant-wide thresholds attached to the tenant's Server subject, keeps from each
/// the type that judges failed fires, and judges all of them against the SCHEDULE as the subject (<see cref="IThresholdEvaluator.Judge"/>).
/// </summary>
/// <remarks>
/// Why the schedule is the subject even for a tenant-wide threshold: the evaluator remembers which rules hold by subject, and the firing of a rule names its subject. Judged against the tenant, one schedule's
/// success would stop another's rule from holding and a firing would not say which schedule failed. Judged against the schedule, each schedule has its own rule state and the firing carries that schedule,
/// which is what <c>PauseSchedule</c> pauses. The evaluator's own contract names this use (<c>Judge</c>: a caller that resolves which thresholds apply by its own rule).
/// </remarks>
public sealed class ScheduleFailureReporter : IScheduleFailureReporter
{
    private const string ScheduleKind = "Schedule";
    private const string ServerKind = "Server";

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ScheduleFailureReporter"/> class.</summary>
    /// <param name="scopes">Gives the scope the thresholds are read and judged in, under the schedule's tenant.</param>
    /// <param name="logger">The logger.</param>
    public ScheduleFailureReporter(IServiceScopeFactory scopes, ILogger<ScheduleFailureReporter>? logger = null)
    {
        _scopes = scopes;
        _logger = logger ?? NullLogger<ScheduleFailureReporter>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Report(Guid scheduleId, string scheduleName, Guid? tenantId, long consecutiveFailures, CancellationToken cancellationToken = default)
    {
        if (tenantId is not { } tenant)
        {
            return GenericResult.Failure(ScheduleFailureLog.TenantMissing(_logger, scheduleId, scheduleName));
        }

        using var scope = _scopes.CreateScope();
        using var asTenant = new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>(), tenant);
        var source = scope.ServiceProvider.GetRequiredService<IThresholdSource>();

        var subject = new ThresholdSubject(ScheduleKind, scheduleName, scheduleId, tenant, null);
        var own = await source.For(subject, cancellationToken).ConfigureAwait(false);
        var wide = await source.For(new ThresholdSubject(ServerKind, ServerKind, tenant, tenant, null), cancellationToken).ConfigureAwait(false);
        if (own.IsFailure || own.Value is not { } ownThresholds)
        {
            return GenericResult.Failure(ScheduleFailureLog.ThresholdsNotRead(_logger, scheduleId, own.CurrentMessage));
        }

        if (wide.IsFailure || wide.Value is not { } wideThresholds)
        {
            return GenericResult.Failure(ScheduleFailureLog.ThresholdsNotRead(_logger, scheduleId, wide.CurrentMessage));
        }

        List<IThresholdDefinition> judged =
        [
            .. ownThresholds.Where(threshold => threshold.Type is ScheduleFailsThresholdType),
            .. wideThresholds.Where(threshold => threshold.Type is ServerScheduleFailsThresholdType),
        ];
        var values = new Dictionary<string, object?>(StringComparer.Ordinal) { [ScheduleFailsThresholdType.ConsecutiveFailures] = consecutiveFailures };
        var report = await scope.ServiceProvider.GetRequiredService<IThresholdEvaluator>().Judge(subject, judged, values, cancellationToken).ConfigureAwait(false);
        if (report.IsFailure || report.Value is not { } judgement)
        {
            return GenericResult.Failure(ScheduleFailureLog.NotJudged(_logger, scheduleId, report.CurrentMessage));
        }

        ScheduleFailureLog.Reported(_logger, scheduleId, scheduleName, consecutiveFailures, judged.Count, judgement.Fired.Count);
        return GenericResult.Success();
    }
}
