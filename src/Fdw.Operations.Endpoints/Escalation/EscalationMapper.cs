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

    /// <summary>Maps an <see cref="UpdateEscalationPolicyRequest"/> to an <see cref="EscalationPolicyModel"/>.</summary>
    public static EscalationPolicyModel ToModel(UpdateEscalationPolicyRequest request)
    {
        return new EscalationPolicyModel
        {
            Id = request.Id,
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
}
