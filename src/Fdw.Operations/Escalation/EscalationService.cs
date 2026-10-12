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
    private const string EscalationPolicyDomain = "EscalationPolicy";

    private readonly IEscalationConfigurationProvider _provider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EscalationService"/> class.
    /// </summary>
    /// <param name="provider">The escalation configuration provider (composes the aggregate on read, cascades on save).</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public EscalationService(
        IEscalationConfigurationProvider provider,
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

    private async Task<IGenericResult<IEscalationPolicy?>> GetPolicyMatching(
        Func<IEscalationPolicyImplementationConfiguration, bool> predicate,
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

        var saved = await _provider
            .Save(BuildConfig(policy, id: null), EscalationPolicyDomain, EscalationPolicyDomain, policy.Name, cancellationToken)
            .ConfigureAwait(false);
        if (!saved.IsSuccess)
            return saved.ToNewResult<IEscalationPolicy>();

        // Save writes the domain row and the implementation together and hands back neither: the
        // domain row's id is minted inside it. Read it back by the name it was written under.
        var written = await _provider.Get(policy.Name, cancellationToken).ConfigureAwait(false);
        if (!written.IsSuccess)
            return written.ToNewResult<IEscalationPolicy>();
        if (written.Value is null)
            return GenericResult<IEscalationPolicy>.Failure(
                OperationsLog.EscalationPersistFailed(_logger, "the policy is not readable by the name it was written under"));

        OperationsLog.EscalationPolicyCreated(_logger, written.Value.Id, policy.Name);
        return GenericResult<IEscalationPolicy>.Success(new EscalationPolicyRecord(written.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IEscalationPolicy>> UpdatePolicy(
        Guid policyId,
        IEscalationPolicy policy,
        CancellationToken cancellationToken = default)
    {
        OperationsLog.EscalationUpdatingPolicy(_logger, policyId);

        var saveResult = await _provider
            .Save(BuildConfig(policy, id: policyId), EscalationPolicyDomain, EscalationPolicyDomain, policy.Name, cancellationToken)
            .ConfigureAwait(false);
        if (!saveResult.IsSuccess)
            return saveResult.ToNewResult<IEscalationPolicy>();

        var updated = await _provider.Get(policy.Name, cancellationToken).ConfigureAwait(false);
        if (!updated.IsSuccess)
            return updated.ToNewResult<IEscalationPolicy>();
        if (updated.Value is null)
            return GenericResult<IEscalationPolicy>.Failure(
                OperationsLog.EscalationPersistFailed(_logger, "the policy is not readable by the name it was written under"));

        OperationsLog.EscalationPolicyUpdated(_logger, policyId);
        return GenericResult<IEscalationPolicy>.Success(new EscalationPolicyRecord(updated.Value));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeletePolicy(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        OperationsLog.EscalationDeletingPolicy(_logger, policyId);

        var result = await _provider.Delete(policyId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return GenericResult.Failure(
                OperationsLog.EscalationPersistFailed(_logger, "Failed to delete policy"));

        OperationsLog.EscalationPolicyDeleted(_logger, policyId);
        return GenericResult.Success();
    }

    private static EscalationPolicyImplementationConfiguration BuildConfig(IEscalationPolicy policy, Guid? id)
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
