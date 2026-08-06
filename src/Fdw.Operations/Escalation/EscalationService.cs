using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Operations.Configuration;
using Fdw.Operations.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Escalation;

/// <summary>
/// CRUD service for managing escalation policies. Delegates all data access to
/// <see cref="EscalationConfigurationProvider"/> — the keystone provider whose base Get composes the
/// Policy→Levels→Recipients aggregate (physical-key cascade) and whose base Save cascades children
/// (version-on-write, physical FK resolution). This service holds only escalation business shaping;
/// it no longer issues raw DataGateway commands or hand-rolls hierarchy loading.
/// </summary>
public sealed class EscalationService : IEscalationService
{
    private readonly EscalationConfigurationProvider _provider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationService"/> class.
    /// </summary>
    /// <param name="provider">The escalation configuration provider (composes the aggregate on read, cascades on save).</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public EscalationService(
        EscalationConfigurationProvider provider,
        ILoggerFactory loggerFactory)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _logger = loggerFactory?.CreateLogger<EscalationService>()
            ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEscalationPolicy>> GetPolicy(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        OperationsLog.EscalationFetchingPolicy(_logger, policyId);

        // Why: provider.Get(id) composes Policy→Levels→Recipients via the base read cascade.
        var result = await _provider.Get(policyId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.ToNewResult<IEscalationPolicy>();
        if (result.Value is null)
            return GenericResult<IEscalationPolicy>.Failure(
                OperationsLog.EscalationPolicyNotFound(_logger, policyId));

        return GenericResult<IEscalationPolicy>.Success(new EscalationPolicyRecord(result.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<IEscalationPolicy>>> GetAllPolicies(
        CancellationToken cancellationToken = default)
    {
        OperationsLog.EscalationListingPolicies(_logger);

        // Why: the list read returns headers only (base Get(ct) does not compose children — that is the
        // keystone's deliberate list-vs-detail split). Re-fetch each enabled policy by id so callers get
        // the composed Levels→Recipients aggregate, matching the prior per-policy hierarchy load.
        var headers = await _provider.Get(cancellationToken).ConfigureAwait(false);
        if (!headers.IsSuccess)
            return GenericResult<IReadOnlyList<IEscalationPolicy>>.Failure(
                OperationsLog.EscalationPersistFailed(_logger, "Failed to query policies"));

        var policies = new List<IEscalationPolicy>();
        foreach (var header in (headers.Value ?? []).Where(c => c.IsEnabled))
        {
            var full = await _provider.Get(header.Id, cancellationToken).ConfigureAwait(false);
            if (!full.IsSuccess)
                return full.ToNewResult<IReadOnlyList<IEscalationPolicy>>();
            if (full.Value is not null)
                policies.Add(new EscalationPolicyRecord(full.Value));
        }

        OperationsLog.EscalationPoliciesFound(_logger, policies.Count);
        return GenericResult<IReadOnlyList<IEscalationPolicy>>.Success(policies);
    }

    /// <inheritdoc />
    public Task<IGenericResult<IEscalationPolicy?>> GetPolicyForWorkflow(
        Guid workflowId,
        CancellationToken cancellationToken = default)
        => GetPolicyMatching(c => c.WorkflowId == workflowId, cancellationToken);

    /// <inheritdoc />
    public Task<IGenericResult<IEscalationPolicy?>> GetPolicyForSchedule(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
        => GetPolicyMatching(c => c.ScheduleId == scheduleId, cancellationToken);

    // Why: workflow/schedule lookups have no dedicated provider verb, so select the matching enabled
    // header from the list read, then compose it by id. Single match expected (one policy per scope).
    private async Task<IGenericResult<IEscalationPolicy?>> GetPolicyMatching(
        Func<EscalationPolicyConfiguration, bool> predicate,
        CancellationToken cancellationToken)
    {
        var headers = await _provider.Get(cancellationToken).ConfigureAwait(false);
        if (!headers.IsSuccess)
            return headers.ToNewResult<IEscalationPolicy?>();

        var header = (headers.Value ?? []).FirstOrDefault(c => c.IsEnabled && predicate(c));
        if (header is null)
            return GenericResult<IEscalationPolicy?>.Success(null);

        var full = await _provider.Get(header.Id, cancellationToken).ConfigureAwait(false);
        if (!full.IsSuccess)
            return full.ToNewResult<IEscalationPolicy?>();

        return GenericResult<IEscalationPolicy?>.Success(
            full.Value is null ? null : new EscalationPolicyRecord(full.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEscalationPolicy>> CreatePolicy(
        IEscalationPolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(policy.Name))
            return GenericResult<IEscalationPolicy>.Failure(
                OperationsLog.EscalationPolicyNameRequired(_logger));

        OperationsLog.EscalationCreatingPolicy(_logger, policy.Name);

        // Why: provider.Save mints the policy Id (Id left empty) and cascade-saves Levels→Recipients,
        // setting the logical {Parent}Id FKs (the translator resolves the physical {Parent}RowId by subquery).
        var result = await _provider.Save(BuildConfig(policy, id: null), cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Value is null)
            return GenericResult<IEscalationPolicy>.Failure(
                OperationsLog.EscalationPersistFailed(_logger, result.CurrentMessage ?? "Failed to create policy"));

        OperationsLog.EscalationPolicyCreated(_logger, result.Value.Id, policy.Name);
        return GenericResult<IEscalationPolicy>.Success(new EscalationPolicyRecord(result.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEscalationPolicy>> UpdatePolicy(
        Guid policyId,
        IEscalationPolicy policy,
        CancellationToken cancellationToken = default)
    {
        OperationsLog.EscalationUpdatingPolicy(_logger, policyId);

        // Why: Save alone now version-on-writes (sets every prior IsCurrent=0, inserts the new
        // version) and cascades the new Levels→Recipients on every write. The Delete-then-Save
        // tombstone workaround this replaced predates that: it targeted a Save that only updated
        // in place unless a prior probe found no current row. Against the now fail-loud Delete
        // (Delete errors when there is no current row instead of silently succeeding), that
        // workaround would abort an update whenever the pre-check raced the row's actual state,
        // even though Save alone would have handled the version-on-write cycle correctly.
        var saveResult = await _provider.Save(BuildConfig(policy, id: policyId), cancellationToken).ConfigureAwait(false);
        if (!saveResult.IsSuccess || saveResult.Value is null)
            return GenericResult<IEscalationPolicy>.Failure(
                OperationsLog.EscalationPersistFailed(_logger, saveResult.CurrentMessage ?? "Failed to insert updated policy version"));

        OperationsLog.EscalationPolicyUpdated(_logger, policyId);
        return GenericResult<IEscalationPolicy>.Success(new EscalationPolicyRecord(saveResult.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeletePolicy(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        OperationsLog.EscalationDeletingPolicy(_logger, policyId);

        // Why: soft-delete the policy header (version-on-write tombstone). Child levels/recipients keep
        // their FK to the now non-current RowId, so subsequent reads (which compose by the current
        // header's RowId) no longer surface them.
        var result = await _provider.Delete(policyId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return GenericResult.Failure(
                OperationsLog.EscalationPersistFailed(_logger, "Failed to delete policy"));

        OperationsLog.EscalationPolicyDeleted(_logger, policyId);
        return GenericResult.Success();
    }

    // Why: map the domain IEscalationPolicy to the persistence config with nested Levels→Recipients.
    // Child Ids are left empty (the save cascade mints them); the {Parent}Id FKs are set by the cascade.
    // id=null for create (base mints the policy Id); id=policyId for update (same logical Id, new version).
    private static EscalationPolicyConfiguration BuildConfig(IEscalationPolicy policy, Guid? id)
        => new()
        {
            Id = id ?? Guid.Empty,
            Name = policy.Name,
            IsEnabled = policy.IsEnabled,
            WorkflowId = policy.WorkflowId,
            MaxEscalationLevel = policy.MaxEscalationLevel,
            CooldownMinutes = policy.CooldownMinutes,
            Levels = policy.Levels.Select(level => new EscalationLevelConfiguration
            {
                Level = level.Level,
                DelayMinutes = level.DelayMinutes,
                NotificationChannel = level.NotificationChannel,
                Template = level.MessageTemplate,
                Severity = "Warning",
                Recipients = level.Recipients.Select(recipient => new EscalationLevelRecipientConfiguration
                {
                    Name = recipient,
                    Recipient = recipient,
                    RecipientType = "Email"
                }).ToList<EscalationLevelRecipientConfiguration>()
            }).ToList<EscalationLevelConfiguration>()
        };
}
