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
            ScheduleId = policy.ScheduleId,
            MaxEscalationLevel = policy.MaxEscalationLevel,
            CooldownMinutes = policy.CooldownMinutes,
            MaxRetries = policy.MaxRetries,
            RetryDelaySeconds = policy.RetryDelaySeconds,
            Levels = policy.Levels.Select(l => new EscalationLevelResponse
            {
                Level = l.Level,
                Name = l.Name,
                DelayMinutes = l.DelayMinutes,
                NotificationChannel = l.NotificationChannel,
                Recipients = ToDto(l.Recipients),
                MessageTemplate = l.MessageTemplate,
                Severity = l.Severity
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
            ScheduleId = request.ScheduleId,
            MaxEscalationLevel = maxEscalationLevel,
            CooldownMinutes = cooldownMinutes,
            MaxRetries = maxRetries,
            RetryDelaySeconds = retryDelaySeconds,
            Levels = request.Levels.Select(l => (IEscalationLevel)new EscalationLevelModel
            {
                Level = l.Level,
                Name = l.Name,
                DelayMinutes = Delay(l),
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate,
                Severity = l.Severity
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
            Name = l.Name,
            DelayMinutes = l.DelayMinutes,
            NotificationChannel = l.NotificationChannel,
            Recipients = ToDto(l.Recipients),
            MessageTemplate = l.MessageTemplate,
            Severity = l.Severity
        }).ToList();

        return new EscalationPolicyModel
        {
            Id = request.Id,
            Name = request.Name ?? existing.Name,
            IsEnabled = request.IsEnabled ?? existing.IsEnabled,
            // A policy has one scope, a workflow or a schedule, and the configuration refuses one with neither (FDW-4814, FDW-4822).
            // An update that states neither keeps the stored scope, so a name-only update of a scoped policy stays valid. One that
            // states a scope sets both members from the request, so naming a schedule on a workflow-scoped policy moves it (the
            // workflow is cleared), and the reverse. A request that states both keeps both as sent; the validator decides.
            WorkflowId = StatesScope(request) ? request.WorkflowId : existing.WorkflowId,
            ScheduleId = StatesScope(request) ? request.ScheduleId : existing.ScheduleId,
            MaxEscalationLevel = request.MaxEscalationLevel ?? existing.MaxEscalationLevel,
            CooldownMinutes = request.CooldownMinutes ?? existing.CooldownMinutes,
            MaxRetries = request.MaxRetries ?? existing.MaxRetries,
            RetryDelaySeconds = request.RetryDelaySeconds ?? existing.RetryDelaySeconds,
            Levels = levels.Select(l => (IEscalationLevel)new EscalationLevelModel
            {
                Level = l.Level,
                Name = l.Name,
                DelayMinutes = Delay(l),
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate,
                Severity = l.Severity
            }).ToList()
        };
    }

    /// <summary>Names what a create request leaves out: the policy's own choices, every level's severity and every recipient's type (FDW-3823).</summary>
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

    /// <summary>Names the name, delay and severity a level leaves out, and the type each of its recipients leaves out. A level with no recipients states no type (FDW-4891).</summary>
    /// <param name="levels">The levels a request sends.</param>
    /// <returns>The missing members, each with its level number.</returns>
    public static IReadOnlyList<string> MissingOnLevels(IReadOnlyList<EscalationLevelResponse>? levels)
    {
        List<string> missing = [];
        foreach (var level in levels ?? [])
        {
            if (string.IsNullOrWhiteSpace(level.Name)) missing.Add($"levels[{level.Level}].name");
            if (level.DelayMinutes is null) missing.Add($"levels[{level.Level}].delayMinutes");
            if (string.IsNullOrWhiteSpace(level.Severity)) missing.Add($"levels[{level.Level}].severity");
            var recipients = level.Recipients ?? [];
            for (var index = 0; index < recipients.Count; index++)
            {
                if (recipients[index] is null) missing.Add($"levels[{level.Level}].recipients[{index}]");
                else if (string.IsNullOrWhiteSpace(recipients[index].RecipientType)) missing.Add($"levels[{level.Level}].recipients[{index}].recipientType");
            }
        }

        return missing;
    }

    private static List<EscalationRecipientResponse> ToDto(IReadOnlyList<IEscalationRecipient> recipients) =>
        recipients.Select(r => new EscalationRecipientResponse { Recipient = r.Recipient, RecipientType = r.RecipientType }).ToList();

    // An update states a scope when it names a workflow or a schedule (FDW-4822).
    private static bool StatesScope(UpdateEscalationPolicyRequest request) => request.WorkflowId is not null || request.ScheduleId is not null;

    // Missing() and MissingOnLevels() have refused a request whose level states no delay before a level is mapped; reaching here without one is a bug, so it is loud.
    private static int Delay(EscalationLevelResponse level)
        => level.DelayMinutes is { } delay
            ? delay
            : throw new System.InvalidOperationException($"Escalation level {level.Level} reached the mapper with no DelayMinutes; the request check should have refused it.");
}
