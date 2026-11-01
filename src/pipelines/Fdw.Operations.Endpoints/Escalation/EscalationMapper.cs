using System.Linq;
using Fdw.Operations.Abstractions.Escalation;

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
            Levels = policy.Levels.Select(l => new EscalationLevelResponse
            {
                Level = l.Level,
                DelayMinutes = l.DelayMinutes,
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate
            }).ToList()
        };
    }

    /// <summary>Maps an <see cref="EscalationPolicyRequest"/> to an <see cref="EscalationPolicyModel"/>.</summary>
    public static EscalationPolicyModel ToModel(EscalationPolicyRequest request)
    {
        return new EscalationPolicyModel
        {
            Name = request.Name,
            IsEnabled = request.IsEnabled,
            WorkflowId = request.WorkflowId,
            MaxEscalationLevel = request.MaxEscalationLevel,
            CooldownMinutes = request.CooldownMinutes,
            Levels = request.Levels.Select(l => (IEscalationLevel)new EscalationLevelModel
            {
                Level = l.Level,
                DelayMinutes = l.DelayMinutes,
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate
            }).ToList()
        };
    }

    /// <summary>
    /// Merges an <see cref="UpdateEscalationPolicyRequest"/> onto <paramref name="existing"/> into a
    /// new <see cref="EscalationPolicyModel"/> — an omitted (null) request property carries the
    /// existing value forward. Levels is the one collection: null means "not sent, keep the existing
    /// set"; an empty list is a real request to clear it.
    /// </summary>
    public static EscalationPolicyModel ToModel(UpdateEscalationPolicyRequest request, IEscalationPolicy existing)
    {
        var levels = request.Levels ?? existing.Levels.Select(l => new EscalationLevelResponse
        {
            Level = l.Level,
            DelayMinutes = l.DelayMinutes,
            NotificationChannel = l.NotificationChannel,
            Recipients = l.Recipients,
            MessageTemplate = l.MessageTemplate
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
            Levels = levels.Select(l => (IEscalationLevel)new EscalationLevelModel
            {
                Level = l.Level,
                DelayMinutes = l.DelayMinutes,
                NotificationChannel = l.NotificationChannel,
                Recipients = l.Recipients,
                MessageTemplate = l.MessageTemplate
            }).ToList()
        };
    }
}
