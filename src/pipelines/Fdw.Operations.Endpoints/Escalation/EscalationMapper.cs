using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Abstractions.Escalation;
using Fdw.Conventions;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>
/// Maps between escalation domain models and DTOs.
/// </summary>
public static class EscalationMapper
{
    /// <summary>Maps an <see cref="IEscalationPolicy"/> to a <see cref="EscalationPolicyResponse"/>.</summary>
    public static EscalationPolicyResponse ToDto(IEscalationPolicy policy)
    {
        return new EscalationPolicyResponse
        {
            Id = policy.Id,
            Name = policy.Name,
            IsEnabled = policy.IsEnabled,
            WorkflowId = policy.WorkflowId,
            MaxEscalationLevel = policy.MaxEscalationLevel,
            CooldownMinutes = policy.CooldownMinutes,
            MaxRetries = policy.MaxRetries,
            RetryDelaySeconds = policy.RetryDelaySeconds,
            Levels = policy.Levels.Select(l => new EscalationLevelResponse
            {
                Level = l.Level,
                DelayMinutes = l.DelayMinutes,
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate,
                Severity = l.Severity,
                RecipientType = l.RecipientType
            }).ToList()
        };
    }

    /// <summary>Maps an <see cref="EscalationPolicyRequest"/> to an <see cref="EscalationPolicyModel"/>.</summary>
    /// <param name="request">The create request.</param>
    /// <param name="isEnabled">Whether the policy is enabled, as the request states it.</param>
    /// <param name="maxEscalationLevel">The maximum level, as the request states it.</param>
    /// <param name="cooldownMinutes">The cooldown, as the request states it.</param>
    /// <param name="maxRetries">The retries, as the request states them.</param>
    /// <param name="retryDelaySeconds">The retry delay, as the request states it.</param>
    public static EscalationPolicyModel ToModel(EscalationPolicyRequest request, bool isEnabled, int maxEscalationLevel, int cooldownMinutes, int maxRetries, int retryDelaySeconds)
    {
        return new EscalationPolicyModel
        {
            Name = request.Name,
            IsEnabled = isEnabled,
            WorkflowId = request.WorkflowId,
            MaxEscalationLevel = maxEscalationLevel,
            CooldownMinutes = cooldownMinutes,
            MaxRetries = maxRetries,
            RetryDelaySeconds = retryDelaySeconds,
            Levels = request.Levels.Select(l => (IEscalationLevel)new EscalationLevelModel
            {
                Level = l.Level,
                DelayMinutes = Delay(l),
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate,
                Severity = l.Severity,
                RecipientType = l.RecipientType
            }).ToList()
        };
    }

    /// <summary>
    /// Merges an <see cref="UpdateEscalationPolicyRequest"/> onto <paramref name="existing"/> into a
    /// new <see cref="EscalationPolicyModel"/> — an omitted (null) request property carries the
    /// existing value forward. Levels is the one collection: null means "not sent, keep the existing
    /// set"; an empty list is a real request to clear it.
    /// </summary>
    [ApprovedFallback("an update that omits a field keeps the stored value")]
    public static EscalationPolicyModel ToModel(UpdateEscalationPolicyRequest request, IEscalationPolicy existing)
    {
        var levels = request.Levels ?? existing.Levels.Select(l => new EscalationLevelResponse
        {
            Level = l.Level,
            DelayMinutes = l.DelayMinutes,
            NotificationChannel = l.NotificationChannel,
            Recipients = l.Recipients,
            MessageTemplate = l.MessageTemplate,
            Severity = l.Severity,
            RecipientType = l.RecipientType
        }).ToList();

        return new EscalationPolicyModel
        {
            Id = request.Id,
            Name = request.Name ?? existing.Name,
            IsEnabled = request.IsEnabled ?? existing.IsEnabled,
            // WorkflowId was already nullable before this fix and is assigned as-sent, not merged:
            // it is itself an optional scope (null legitimately means "no workflow"), so unlike the
            // other properties here there is no wire representation that distinguishes "omitted,
            // leave alone" from "explicitly cleared" for it. Not in scope of the fields the audit
            // found broken -- left exactly as it behaved before.
            WorkflowId = request.WorkflowId,
            MaxEscalationLevel = request.MaxEscalationLevel ?? existing.MaxEscalationLevel,
            CooldownMinutes = request.CooldownMinutes ?? existing.CooldownMinutes,
            MaxRetries = request.MaxRetries ?? existing.MaxRetries,
            RetryDelaySeconds = request.RetryDelaySeconds ?? existing.RetryDelaySeconds,
            Levels = levels.Select(l => (IEscalationLevel)new EscalationLevelModel
            {
                Level = l.Level,
                DelayMinutes = Delay(l),
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate,
                Severity = l.Severity,
                RecipientType = l.RecipientType
            }).ToList()
        };
    }

    /// <summary>Names what a create request leaves out: the policy's own choices and every level's severity and recipient type (FDW-3823).</summary>
    /// <param name="request">The create request.</param>
    /// <returns>The missing members, empty when the request states them all.</returns>
    public static IReadOnlyList<string> Missing(EscalationPolicyRequest request)
    {
        List<string> missing = [];
        if (request.IsEnabled is null) missing.Add("isEnabled");
        if (request.MaxEscalationLevel is null) missing.Add("maxEscalationLevel");
        if (request.CooldownMinutes is null) missing.Add("cooldownMinutes");
        if (request.MaxRetries is null) missing.Add("maxRetries");
        if (request.RetryDelaySeconds is null) missing.Add("retryDelaySeconds");
        missing.AddRange(MissingOnLevels(request.Levels));
        return missing;
    }

    /// <summary>Names the severity and recipient type a level leaves out.</summary>
    /// <param name="levels">The levels a request sends.</param>
    /// <returns>The missing members, each with its level number.</returns>
    public static IReadOnlyList<string> MissingOnLevels(IReadOnlyList<EscalationLevelResponse>? levels)
    {
        List<string> missing = [];
        foreach (var level in levels ?? [])
        {
            if (level.DelayMinutes is null) missing.Add($"levels[{level.Level}].delayMinutes");
            if (string.IsNullOrWhiteSpace(level.Severity)) missing.Add($"levels[{level.Level}].severity");
            if (string.IsNullOrWhiteSpace(level.RecipientType)) missing.Add($"levels[{level.Level}].recipientType");
        }

        return missing;
    }

    // Missing() and MissingOnLevels() have refused a request whose level states no delay before a level is mapped; reaching here without one is a bug, so it is loud.
    private static int Delay(EscalationLevelResponse level)
        => level.DelayMinutes is { } delay
            ? delay
            : throw new System.InvalidOperationException($"Escalation level {level.Level} reached the mapper with no DelayMinutes; the request check should have refused it.");
}
