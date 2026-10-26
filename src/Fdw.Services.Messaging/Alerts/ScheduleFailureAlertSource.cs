using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Messaging.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Messaging;

/// <summary>
/// The kind of alert a failed run is (FDW-2431): the run in the failure history of the scheduler, which a person can acknowledge, snooze, silence,
/// dismiss or discard like a message or a UI comment.
/// </summary>
/// <remarks>
/// <para>
/// The alert's identifier is the run's. A run is an alert only when it is a run (a Job, as the list of executions holds) and it has failed; a run in any
/// other state, a stage of a run, or an identifier that names no execution is not an alert.
/// </para>
/// <para>
/// The alert adds no right. The alert routes ask only for a signed-in person, so this source asks for the right that already lets a person read the failures:
/// <c>pipelines:read</c>, the policy of the list of executions. The tracker reads as the caller, so a run of a tenant the caller does not reach is not found.
/// </para>
/// <para>
/// What a person does to a run is recorded as an alert action and nothing else changes: the run stays as it was recorded.
/// </para>
/// </remarks>
public sealed class ScheduleFailureAlertSource : IAlertSource
{
    // The right that lets a person read the failures: the policy of the list of executions.
    private const string ReadPermission = "pipelines:read";

    private readonly ILogger<ScheduleFailureAlertSource> _logger;
    private readonly IExecutionTracker? _tracker;
    private readonly IAuthenticationContextAccessor _authContext;

    /// <summary>Initializes a new instance of the <see cref="ScheduleFailureAlertSource"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authContext">Supplies the caller, whose rights say whether they may see a failure.</param>
    /// <param name="tracker">Reads the run, as the caller. A host that tracks no executions has none, and the kind then fails when it is asked for a run.</param>
    public ScheduleFailureAlertSource(
        ILogger<ScheduleFailureAlertSource>? logger, IAuthenticationContextAccessor authContext, IExecutionTracker? tracker = null)
    {
        _logger = logger ?? NullLogger<ScheduleFailureAlertSource>.Instance;
        ArgumentNullException.ThrowIfNull(authContext);
        _authContext = authContext;
        _tracker = tracker;
    }

    /// <inheritdoc />
    public string Kind => ScheduleFailureAlert.Kind;

    /// <inheritdoc />
    public IReadOnlyList<string> OfferedActions { get; } = ["Acknowledge", "Snooze", "Silence", "Dismiss", "Discard"];

    /// <inheritdoc />
    public async Task<IGenericResult<AlertSubject?>> Find(Guid id, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "Find an alert of a failed run");

        try
        {
            if (_tracker is null)
                return GenericResult<AlertSubject?>.Failure(MessagingLog.ScheduleFailureNeedsExecutions(_logger));

            if (_authContext.Current is not { } context || !PermissionCoverage.Covers(context.Permissions, ReadPermission))
                return GenericResult<AlertSubject?>.Success(null);

            var found = await _tracker.GetItem(id, cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
                return FailureCategory.IsMissing(found) ? GenericResult<AlertSubject?>.Success(null) : found.ToNewResult<AlertSubject?>();

            var item = found.Value!;
            return GenericResult<AlertSubject?>.Success(IsFailedRun(item) ? new AlertSubject(item.Id, item.TenantId) : null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<AlertSubject?>.Failure(MessagingLog.AlertUnexpected(_logger, "find an alert of a failed run", ex.Message));
        }
    }

    /// <inheritdoc />
    /// <remarks>Nothing happens to the run when a person acts: the action is its whole record.</remarks>
    public Task<IGenericResult> Acted(Guid id, IAlertAction action, CancellationToken cancellationToken = default)
    {
        MessagingLog.TraceAlertEntry(_logger, "Act on an alert of a failed run");
        return Task.FromResult(GenericResult.Success());
    }

    // A run is what the list of executions holds by default (a Job); it is an alert once it has failed.
    private static bool IsFailedRun(IExecutionItem item) =>
        string.Equals(item.ItemType.Name, ExecutionItemTypes.Job.Name, StringComparison.Ordinal)
        && string.Equals(item.State.Name, ExecutionStateTypes.Failed.Name, StringComparison.Ordinal);
}
