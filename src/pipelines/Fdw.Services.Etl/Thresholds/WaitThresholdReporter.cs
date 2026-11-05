using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Tells the one threshold evaluator how many runs are waiting to start (FDW-3975), for each pipeline and each schedule that has runs waiting and for the server, so the
/// PipelineWaits, ScheduleWaits and ServerWaits thresholds attached to them judge their rules and fire their consequence (an alert on an escalation policy, or notification rules).
/// </summary>
/// <remarks>
/// <para>
/// Why a subject that has just run out of waiting runs is reported once with 0: a rule that fires on entry fires when its criteria start to hold and holds its fire until they stop,
/// and the evaluator can only see that they stopped if it is told the count that stopped them.
/// </para>
/// <para>
/// The server has no domain row: the thresholds of the server belong to a tenant, and the subject is the server in that tenant, judged with every run that waits on the server
/// (the waiting runs of all tenants, whether or not they name one) for each tenant that has a run waiting. A tenant whose own runs have drained is reported once more with the
/// count the server still has, not with 0, since the server's runs have not.
/// </para>
/// <para>
/// A waiting run that names no tenant is not counted for a pipeline or schedule and is logged: a subject needs the tenant its alert would open under, and none is assumed. A run that names a schedule that
/// carries no schedule Id is counted for its pipeline only, and is logged. This is called from the one loop of <see cref="WaitThresholdMonitor"/>, so it keeps the
/// subjects it last reported without a lock.
/// </para>
/// </remarks>
public sealed class WaitThresholdReporter
{
    private const string PipelineKind = "Pipeline";

    private const string ScheduleKind = "Schedule";

    private const string ServerKind = "Server";

    private readonly IThresholdEvaluator _evaluator;
    private readonly ILogger _logger;
    private Dictionary<WaitSubject, Guid> _reported = [];

    // What a count is of: a pipeline or a schedule, by name, in a tenant.
    private readonly record struct WaitSubject(string Kind, string Name, Guid TenantId);

    /// <summary>Initializes a new instance of the <see cref="WaitThresholdReporter"/> class.</summary>
    /// <param name="evaluator">The evaluator that judges the thresholds attached to a subject.</param>
    /// <param name="logger">The logger.</param>
    public WaitThresholdReporter(IThresholdEvaluator evaluator, ILogger<WaitThresholdReporter>? logger = null)
    {
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _logger = logger ?? NullLogger<WaitThresholdReporter>.Instance;
    }

    /// <summary>Reports the waiting runs of each pipeline and schedule in a snapshot of the queue.</summary>
    /// <param name="snapshot">The queue as it stood after a change.</param>
    /// <param name="cancellationToken">Cancels the report.</param>
    /// <returns>A task that ends when every subject has been reported.</returns>
    public async Task Report(PipelineRunQueueSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var waiting = new Dictionary<WaitSubject, (Guid SubjectId, long Count)>();
        CountPipelines(snapshot, waiting);
        CountSchedules(snapshot, waiting);
        CountServer(snapshot, waiting);
        foreach (var subject in waiting)
        {
            await Send(subject.Key, subject.Value.SubjectId, subject.Value.Count, cancellationToken).ConfigureAwait(false);
        }

        foreach (var drained in _reported.Where(pair => !waiting.ContainsKey(pair.Key)).ToList())
        {
            await Send(drained.Key, drained.Value, string.Equals(drained.Key.Kind, ServerKind, StringComparison.Ordinal) ? snapshot.Waiting.Count : 0, cancellationToken).ConfigureAwait(false);
        }

        _reported = waiting.ToDictionary(pair => pair.Key, pair => pair.Value.SubjectId);
    }

    private void CountPipelines(PipelineRunQueueSnapshot snapshot, Dictionary<WaitSubject, (Guid SubjectId, long Count)> counted)
    {
        foreach (var run in snapshot.Waiting.Select(entry => entry.Run))
        {
            if (run.TenantId is not { } tenantId)
            {
                WaitThresholdLog.WaitingRunHasNoTenant(_logger, run.PipelineName);
                continue;
            }

            Add(counted, new WaitSubject(PipelineKind, run.PipelineName, tenantId), run.PipelineId);
        }
    }

    private void CountSchedules(PipelineRunQueueSnapshot snapshot, Dictionary<WaitSubject, (Guid SubjectId, long Count)> counted)
    {
        foreach (var run in snapshot.Waiting.Select(entry => entry.Run))
        {
            if (run.TenantId is not { } tenantId || run.ScheduleName is not { } schedule)
            {
                continue;
            }

            if (run.ScheduleId is not { } scheduleId)
            {
                WaitThresholdLog.WaitingRunHasNoScheduleId(_logger, run.PipelineName, schedule);
                continue;
            }

            Add(counted, new WaitSubject(ScheduleKind, schedule, tenantId), scheduleId);
        }
    }

    private static void CountServer(PipelineRunQueueSnapshot snapshot, Dictionary<WaitSubject, (Guid SubjectId, long Count)> counted)
    {
        foreach (var tenantId in snapshot.Waiting.Select(entry => entry.Run.TenantId).OfType<Guid>().Distinct())
        {
            counted[new WaitSubject(ServerKind, ServerKind, tenantId)] = (tenantId, snapshot.Waiting.Count);
        }
    }

    private static void Add(Dictionary<WaitSubject, (Guid SubjectId, long Count)> counted, WaitSubject subject, Guid subjectId) =>
        counted[subject] = counted.TryGetValue(subject, out var seen) ? (seen.SubjectId, seen.Count + 1) : (subjectId, 1);

    private async Task Send(WaitSubject waiting, Guid subjectId, long waitingRuns, CancellationToken cancellationToken)
    {
        var subject = new ThresholdSubject(waiting.Kind, waiting.Name, subjectId, waiting.TenantId, null);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal) { [PipelineWaitsThresholdType.WaitingRuns] = waitingRuns };
        try
        {
            var report = await _evaluator.Report(subject, values, cancellationToken).ConfigureAwait(false);
            if (report.IsFailure)
            {
                WaitThresholdLog.WaitingRunsNotJudged(_logger, waiting.Name, report.CurrentMessage);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            WaitThresholdLog.WaitingRunsReportFailed(_logger, ex, waiting.Name);
        }
    }
}
