using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Data;
using Fdw.Operations.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Operations.Escalation;

/// <summary>
/// The alerts escalation policies walk (FDW-3863): opens one for a threshold that fired, sends the levels of its policy as they fall due, and lets a person acknowledge or
/// resolve it. The rules of timing are <see cref="EscalationTimeline"/>; the alerts are kept by <see cref="IEscalationAlertStore"/>.
/// </summary>
/// <remarks>
/// Nothing is assumed. A policy that is missing, not enabled or without a level within its maximum is a failure naming it; a level is sent only through the notification service
/// it names and with the priority the setting <see cref="SettingDefinitions.EscalationNotificationPriority"/> states, and a level that cannot be sent is not recorded as sent, so
/// the next pass sends it again.
/// </remarks>
public sealed class EscalationAlerts : IEscalationAlerts
{
    private readonly IEscalationAlertStore _store;
    private readonly IEscalationService _policies;
    private readonly INotificationServiceProvider _notifications;
    private readonly IEffectiveSettingsProvider _settings;
    private readonly IAuthenticationContextAccessor _authContext;
    private readonly TimeProvider _clock;
    private readonly ILogger _logger;

    internal EscalationAlerts(
        IEscalationAlertStore store,
        IEscalationService policies,
        INotificationServiceProvider notifications,
        IEffectiveSettingsProvider settings,
        IAuthenticationContextAccessor authContext,
        TimeProvider clock,
        ILogger? logger = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _policies = policies ?? throw new ArgumentNullException(nameof(policies));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _authContext = authContext ?? throw new ArgumentNullException(nameof(authContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EscalationOpened>> Open(EscalationAlertRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var policy = await PolicyNamed(request.PolicyName, cancellationToken).ConfigureAwait(false);
        if (policy.IsFailure || policy.Value is null)
        {
            return policy.ToNewResult<EscalationOpened>();
        }

        var history = await _store.OfThreshold(request.TenantId, request.SubjectKind, request.SubjectName, request.ThresholdName, cancellationToken).ConfigureAwait(false);
        if (history.IsFailure || history.Value is null)
        {
            return history.ToNewResult<EscalationOpened>();
        }

        if (history.Value.FirstOrDefault(row => !string.Equals(row.State, EscalationAlertStates.Resolved, StringComparison.Ordinal)) is { } open)
        {
            EscalationAlertLog.AlertAlreadyOpen(_logger, request.ThresholdName, request.SubjectKind, request.SubjectName, open.Id);
            return GenericResult<EscalationOpened>.Success(new EscalationOpened(EscalationOpenOutcomes.AlreadyOpen, Of(open)));
        }

        var now = _clock.GetUtcNow();
        if (!EscalationTimeline.CooldownPassed(policy.Value, history.Value.Select(row => row.ResolvedAt).Max(), now))
        {
            EscalationAlertLog.AlertCoolingDown(_logger, request.ThresholdName, request.SubjectKind, request.SubjectName, request.PolicyName);
            return GenericResult<EscalationOpened>.Success(new EscalationOpened(EscalationOpenOutcomes.CoolingDown, null));
        }

        return await Fire(request, now, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<int>> Advance(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var open = await _store.List(EscalationAlertStates.Firing, cancellationToken).ConfigureAwait(false);
        if (open.IsFailure || open.Value is null)
        {
            return open.ToNewResult<int>();
        }

        var policies = await _policies.GetEnabledPolicies(cancellationToken).ConfigureAwait(false);
        if (policies.IsFailure || policies.Value is null)
        {
            return policies.ToNewResult<int>();
        }

        var sent = 0;
        foreach (var row in open.Value)
        {
            sent += await AdvanceOne(row, policies.Value, now, cancellationToken).ConfigureAwait(false);
        }

        return GenericResult<int>.Success(sent);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EscalationAlert>> Acknowledge(Guid alertId, string acknowledgedBy, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(acknowledgedBy);
        var found = await Reachable(alertId, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } row)
        {
            return found.ToNewResult<EscalationAlert>();
        }

        if (!string.Equals(row.State, EscalationAlertStates.Firing, StringComparison.Ordinal))
        {
            return GenericResult<EscalationAlert>.Failure(EscalationAlertLog.AlertStateRefused(_logger, alertId, row.State, "an acknowledge"));
        }

        row.State = EscalationAlertStates.Acknowledged;
        row.AcknowledgedBy = acknowledgedBy;
        row.AcknowledgedAt = _clock.GetUtcNow();
        var saved = await Save(row, cancellationToken).ConfigureAwait(false);
        if (saved.IsSuccess)
        {
            EscalationAlertLog.AlertAcknowledged(_logger, alertId, acknowledgedBy);
        }

        return saved;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<EscalationAlert>> Resolve(Guid alertId, string resolvedBy, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedBy);
        var found = await Reachable(alertId, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } row)
        {
            return found.ToNewResult<EscalationAlert>();
        }

        if (string.Equals(row.State, EscalationAlertStates.Resolved, StringComparison.Ordinal))
        {
            return GenericResult<EscalationAlert>.Failure(EscalationAlertLog.AlertStateRefused(_logger, alertId, row.State, "a resolve"));
        }

        row.State = EscalationAlertStates.Resolved;
        row.ResolvedBy = resolvedBy;
        row.ResolvedAt = _clock.GetUtcNow();
        var saved = await Save(row, cancellationToken).ConfigureAwait(false);
        if (saved.IsSuccess)
        {
            EscalationAlertLog.AlertResolved(_logger, alertId, resolvedBy);
        }

        return saved;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<EscalationAlert>>> List(string? state = null, CancellationToken cancellationToken = default)
    {
        if (state is not null && !States.Contains(state, StringComparer.Ordinal))
        {
            return GenericResult<IReadOnlyList<EscalationAlert>>.Failure(EscalationAlertLog.StateUnknown(_logger, state));
        }

        var listed = await _store.List(state, cancellationToken).ConfigureAwait(false);
        if (listed.IsFailure || listed.Value is null)
        {
            return listed.ToNewResult<IReadOnlyList<EscalationAlert>>();
        }

        var scope = ExecutionTenantScope.For(_authContext.Current);
        return GenericResult<IReadOnlyList<EscalationAlert>>.Success([.. listed.Value.Where(row => scope.Reaches(row.TenantId)).Select(Of)]);
    }

    private static readonly string[] States = [EscalationAlertStates.Firing, EscalationAlertStates.Acknowledged, EscalationAlertStates.Resolved];

    private async Task<IGenericResult<EscalationOpened>> Fire(EscalationAlertRequest request, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var row = new EscalationAlertRow
        {
            TenantId = request.TenantId,
            SubjectKind = request.SubjectKind,
            SubjectName = request.SubjectName,
            ThresholdName = request.ThresholdName,
            PolicyName = request.PolicyName,
            State = EscalationAlertStates.Firing,
            CurrentLevel = 0,
            LevelReachedAt = now,
            FiredAt = now,
            Subject = request.Subject,
            Message = request.Message,
        };
        var added = await _store.Add(row, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.ToNewResult<EscalationOpened>();
        }

        EscalationAlertLog.AlertOpened(_logger, row.Id, request.ThresholdName, request.SubjectKind, request.SubjectName, request.PolicyName);
        return GenericResult<EscalationOpened>.Success(new EscalationOpened(EscalationOpenOutcomes.Opened, Of(row)));
    }

    private async Task<IGenericResult<IEscalationPolicy>> PolicyNamed(string name, CancellationToken cancellationToken)
    {
        var all = await _policies.GetAllPolicies(cancellationToken).ConfigureAwait(false);
        return all.IsFailure || all.Value is null ? all.ToNewResult<IEscalationPolicy>() : Usable(all.Value, name);
    }

    private IGenericResult<IEscalationPolicy> Usable(IReadOnlyList<IEscalationPolicy> policies, string name)
    {
        if (policies.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal)) is not { } policy)
        {
            return GenericResult<IEscalationPolicy>.Failure(EscalationAlertLog.PolicyNotFound(_logger, name));
        }

        if (!policy.IsEnabled)
        {
            return GenericResult<IEscalationPolicy>.Failure(EscalationAlertLog.PolicyNotEnabled(_logger, name));
        }

        return EscalationTimeline.Next(policy, 0) is null
            ? GenericResult<IEscalationPolicy>.Failure(EscalationAlertLog.PolicyHasNoLevel(_logger, name))
            : GenericResult<IEscalationPolicy>.Success(policy);
    }

    // Sends every level of the alert that has fallen due. A level that cannot be sent, or whose sending cannot be recorded, stops the alert for this pass; the next pass tries again.
    private async Task<int> AdvanceOne(EscalationAlertRow row, IReadOnlyList<IEscalationPolicy> policies, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var found = Usable(policies, row.PolicyName);
        if (found.IsFailure || found.Value is not { } policy)
        {
            return 0;
        }

        var count = 0;
        while (EscalationTimeline.Due(policy, row.CurrentLevel, row.LevelReachedAt, now) is { } step)
        {
            if ((await Send(row, policy, step, cancellationToken).ConfigureAwait(false)).IsFailure)
            {
                return count;
            }

            row.CurrentLevel = step.Level;
            row.LevelReachedAt = now;
            if ((await _store.Update(row, cancellationToken).ConfigureAwait(false)).IsFailure)
            {
                return count;
            }

            count++;
        }

        return count;
    }

    private async Task<IGenericResult> Send(EscalationAlertRow row, IEscalationPolicy policy, EscalationStep step, CancellationToken cancellationToken)
    {
        var level = step.Definition;
        if (!_settings.TryGetEffectiveValue<string>(SettingDefinitions.EscalationNotificationPriority, out var priorityName, row.TenantId)
            || ReferenceEquals(NotificationPriorities.ByName(priorityName), NotificationPriorities.NotFound))
        {
            return GenericResult.Failure(EscalationAlertLog.PrioritySettingMissing(_logger, row.Id, step.Level, SettingDefinitions.EscalationNotificationPriority, priorityName));
        }

        var served = await _notifications.Get(level.NotificationChannel, cancellationToken).ConfigureAwait(false);
        if (!served.IsSuccess || served.Value is not INotificationService service)
        {
            return GenericResult.Failure(EscalationAlertLog.LevelNotSent(_logger, row.Id, step.Level, policy.Name, level.NotificationChannel, "no notification service has that name"));
        }

        var sent = await service.Send(
            new NotificationRequest(
                service.ServiceType, [.. level.Recipients.Select(recipient => recipient.Recipient)], EscalationMessage.SubjectOf(row, step.Level), EscalationMessage.BodyOf(row, level),
                NotificationPriorities.ByName(priorityName),
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["AlertId"] = row.Id, ["Policy"] = policy.Name, ["Level"] = step.Level, ["Threshold"] = row.ThresholdName }),
            cancellationToken).ConfigureAwait(false);
        if (sent.IsFailure)
        {
            return GenericResult.Failure(EscalationAlertLog.LevelNotSent(_logger, row.Id, step.Level, policy.Name, level.NotificationChannel, sent.CurrentMessage));
        }

        EscalationAlertLog.LevelSent(_logger, row.Id, step.Level, policy.Name, level.NotificationChannel);
        return GenericResult.Success();
    }

    // The alert, when the caller's tenant reaches it; an alert of another tenant answers as one that does not exist.
    private async Task<IGenericResult<EscalationAlertRow>> Reachable(Guid alertId, CancellationToken cancellationToken)
    {
        var found = await _store.Find(alertId, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.ToNewResult<EscalationAlertRow>();
        }

        return found.Value is { } row && ExecutionTenantScope.For(_authContext.Current).Reaches(row.TenantId)
            ? GenericResult<EscalationAlertRow>.Success(row)
            : GenericResult<EscalationAlertRow>.Failure(EscalationAlertLog.AlertNotFound(_logger, alertId));
    }

    private async Task<IGenericResult<EscalationAlert>> Save(EscalationAlertRow row, CancellationToken cancellationToken)
    {
        var saved = await _store.Update(row, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<EscalationAlert>() : GenericResult<EscalationAlert>.Success(Of(row));
    }

    private static EscalationAlert Of(EscalationAlertRow row) => new(
        row.Id, row.TenantId, row.SubjectKind, row.SubjectName, row.ThresholdName, row.PolicyName, row.State, row.CurrentLevel, row.LevelReachedAt, row.FiredAt,
        row.AcknowledgedBy, row.AcknowledgedAt, row.ResolvedBy, row.ResolvedAt, row.Subject, row.Message);
}
