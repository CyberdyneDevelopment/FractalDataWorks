using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>
/// Answers whether a run that arrives is taken (FDW-3975): it asks the one threshold evaluator which HoldNewRuns rules of the thresholds attached to the owner the levels
/// chose hold, with the waiting runs of that owner. A rule that holds refuses the run while its criteria hold; nothing is fired, since a hold is a state and not an event.
/// </summary>
/// <remarks>
/// <para>
/// The owner is the one <see cref="WaitThresholdResolver"/> chose: the run's schedule, its pipeline, or the server. A level whose mode applies no thresholds takes every run.
/// The server's subject is the server in the run's tenant, as the reporter's is, and counts every run waiting on the server.
/// </para>
/// <para>
/// A run that names no tenant, or a schedule that is not one of the schedules read, cannot be asked about, so it is not taken and the reason is logged. Nothing is assumed.
/// </para>
/// <para>
/// Why it opens a scope for each check (FDW-4611): this is a singleton, and the threshold source and the evaluator are scoped (the evaluator hands the scope's services to the
/// consequence handlers). A singleton that took them would keep the one it was built with for the life of the process. The check runs in the flow of whoever triggered the run,
/// whose ambient tenant is that caller's; the run's thresholds are the run's tenant's, so the scope works under the run's own tenant, as the stored requests of the runs do.
/// </para>
/// </remarks>
public sealed class WaitHoldGate
{
    private const string HoldNewRuns = "HoldNewRuns";

    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="WaitHoldGate"/> class.</summary>
    /// <param name="scopes">Opens the scope each check resolves the threshold source and the evaluator in.</param>
    /// <param name="logger">The logger.</param>
    public WaitHoldGate(IServiceScopeFactory scopes, ILogger<WaitHoldGate>? logger = null)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? NullLogger<WaitHoldGate>.Instance;
    }

    /// <summary>Says whether a run that arrives is taken.</summary>
    /// <param name="request">The run that arrives.</param>
    /// <param name="owner">The level the run's levels chose.</param>
    /// <param name="snapshot">The queue as it last stood.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Open, or the threshold and the reason that refuse the run; a failure when the run cannot be asked about.</returns>
    public async Task<IGenericResult<PipelineRunStopCheck>> Check(
        PipelineExecutionRequest request, WaitThresholdLayer owner, PipelineRunQueueSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!owner.Mode.StatesThresholds)
        {
            return GenericResult<PipelineRunStopCheck>.Success(PipelineRunStopCheck.Open);
        }

        if (request.TenantId is not { } tenantId)
        {
            return GenericResult<PipelineRunStopCheck>.Failure(WaitThresholdLog.HoldRunHasNoTenant(_logger, request.PipelineName));
        }

        var asked = Subject(request, owner, tenantId, snapshot);
        if (asked.IsFailure || asked.Value is not { } subject)
        {
            return asked.ToNewResult<PipelineRunStopCheck>();
        }

        // The scope and the tenant live for the rest of the check: the source reads under the run's tenant and the evaluator's handlers are this scope's.
        using var scope = _scopes.CreateScope();
        using var tenant = new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>(), tenantId);
        var source = scope.ServiceProvider.GetRequiredService<IThresholdSource>();
        var evaluator = scope.ServiceProvider.GetRequiredService<IThresholdEvaluator>();

        var attached = await source.For(subject.Subject, cancellationToken).ConfigureAwait(false);
        if (attached.IsFailure)
        {
            return attached.ToNewResult<PipelineRunStopCheck>();
        }

        // Why a list that is not there is a failure: the source answers a list, empty for a subject with no threshold; none at all is a source that did not say, and a run is
        // not taken on what was not said.
        if (attached.Value is null)
        {
            return GenericResult<PipelineRunStopCheck>.Failure(WaitThresholdLog.HoldThresholdsNotRead(_logger, request.PipelineName, subject.Subject.Kind));
        }

        var values = new Dictionary<string, object?>(StringComparer.Ordinal) { [PipelineWaitsThresholdType.WaitingRuns] = subject.Waiting };
        var held = evaluator.Holds(subject.Subject, attached.Value, values, HoldNewRuns);
        if (held.Count == 0)
        {
            return GenericResult<PipelineRunStopCheck>.Success(PipelineRunStopCheck.Open);
        }

        WaitThresholdLog.RunStopped(_logger, request.PipelineName, held[0].ThresholdName, owner.Owner);
        return GenericResult<PipelineRunStopCheck>.Success(new PipelineRunStopCheck(true, held[0].ThresholdName, $"The server is not taking new runs: {held[0].Reason}"));
    }

    // The subject the owner is and the runs of it that wait.
    private sealed record Asked(ThresholdSubject Subject, long Waiting);

    private IGenericResult<Asked> Subject(PipelineExecutionRequest request, WaitThresholdLayer owner, Guid tenantId, PipelineRunQueueSnapshot snapshot)
    {
        var waiting = snapshot.Waiting.Select(entry => entry.Run).ToList();
        if (string.Equals(KindOf(owner), WaitThresholdOwners.Pipeline, StringComparison.Ordinal))
        {
            var pipeline = new ThresholdSubject(WaitThresholdOwners.Pipeline, request.PipelineName, request.PipelineId, tenantId, null);
            return GenericResult<Asked>.Success(new Asked(pipeline, waiting.Count(run => run.PipelineId == request.PipelineId && run.TenantId == tenantId)));
        }

        if (string.Equals(KindOf(owner), WaitThresholdOwners.Schedule, StringComparison.Ordinal))
        {
            return ScheduleSubject(request, tenantId, waiting);
        }

        var server = new ThresholdSubject(WaitThresholdOwners.Server, WaitThresholdOwners.Server, tenantId, tenantId, null);
        return GenericResult<Asked>.Success(new Asked(server, waiting.Count));
    }

    // The schedule is the one the request carries: its name and its Id come from the trigger that read the schedule.
    private IGenericResult<Asked> ScheduleSubject(PipelineExecutionRequest request, Guid tenantId, List<PipelineRunIdentity> waiting)
    {
        if (request.ScheduleName is not { } schedule || request.ScheduleId is not { } scheduleId)
        {
            return GenericResult<Asked>.Failure(WaitThresholdLog.HoldScheduleIdMissing(_logger, request.PipelineName, request.ScheduleName ?? string.Empty));
        }

        var counted = waiting.Count(run => run.TenantId == tenantId && run.ScheduleId == scheduleId);
        return GenericResult<Asked>.Success(new Asked(new ThresholdSubject(WaitThresholdOwners.Schedule, schedule, scheduleId, tenantId, null), counted));
    }

    // A level's Owner is its kind alone for the server, and the kind, a colon and the name for a pipeline or a schedule.
    private static string KindOf(WaitThresholdLayer owner)
    {
        var separator = owner.Owner.IndexOf(':', StringComparison.Ordinal);
        return separator < 0 ? owner.Owner : owner.Owner[..separator];
    }
}
