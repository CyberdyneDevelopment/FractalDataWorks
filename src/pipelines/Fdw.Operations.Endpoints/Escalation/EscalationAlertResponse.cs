using System;
using Fdw.Operations.Abstractions.Escalation;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>One escalation alert as the alerts routes answer it (FDW-3863). Every member is always present; the ones that do not apply are null.</summary>
public sealed class EscalationAlertResponse
{
    /// <summary>Gets or sets the alert's id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the threshold belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets what kind of owner fired: Server, Pipeline, Schedule or Component.</summary>
    public string SubjectKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner's name.</summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>Gets or sets the threshold's name.</summary>
    public string ThresholdName { get; set; } = string.Empty;

    /// <summary>Gets or sets the escalation policy the alert follows.</summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the state: Firing, Acknowledged or Resolved.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the highest level sent so far; 0 when none has been.</summary>
    public int CurrentLevel { get; set; }

    /// <summary>Gets or sets when that level was sent; when the alert fired while none has been.</summary>
    public DateTimeOffset LevelReachedAt { get; set; }

    /// <summary>Gets or sets when the threshold fired.</summary>
    public DateTimeOffset FiredAt { get; set; }

    /// <summary>Gets or sets who acknowledged the alert; null while nobody has.</summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>Gets or sets when it was acknowledged.</summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }

    /// <summary>Gets or sets who resolved the alert; null while it is open.</summary>
    public string? ResolvedBy { get; set; }

    /// <summary>Gets or sets when it was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Gets or sets the one-line subject its notifications carry.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets or sets the body its notifications carry.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Maps an alert to its response.</summary>
    /// <param name="alert">The alert.</param>
    /// <returns>The response.</returns>
    public static EscalationAlertResponse Of(EscalationAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);
        return new EscalationAlertResponse
        {
            Id = alert.Id, TenantId = alert.TenantId, SubjectKind = alert.SubjectKind, SubjectName = alert.SubjectName, ThresholdName = alert.ThresholdName,
            PolicyName = alert.PolicyName, State = alert.State, CurrentLevel = alert.CurrentLevel, LevelReachedAt = alert.LevelReachedAt, FiredAt = alert.FiredAt,
            AcknowledgedBy = alert.AcknowledgedBy, AcknowledgedAt = alert.AcknowledgedAt, ResolvedBy = alert.ResolvedBy, ResolvedAt = alert.ResolvedAt,
            Subject = alert.Subject, Message = alert.Message,
        };
    }
}
