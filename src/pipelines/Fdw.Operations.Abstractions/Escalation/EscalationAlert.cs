using System;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>
/// One alert an escalation policy is walking (FDW-3863): what fired, the policy it follows, where it is in the policy's levels and who has acknowledged or resolved it.
/// </summary>
/// <param name="Id">The alert's id.</param>
/// <param name="TenantId">The tenant the threshold belongs to.</param>
/// <param name="SubjectKind">What kind of owner fired: Server, Pipeline, Schedule or Component.</param>
/// <param name="SubjectName">The owner's name.</param>
/// <param name="ThresholdName">The threshold's name.</param>
/// <param name="PolicyName">The escalation policy it follows, by the name it had when the alert fired.</param>
/// <param name="State">One of <see cref="EscalationAlertStates"/>.</param>
/// <param name="CurrentLevel">The highest level sent so far; 0 when none has been.</param>
/// <param name="LevelReachedAt">When that level was sent; when the alert fired while none has been.</param>
/// <param name="FiredAt">When the threshold fired.</param>
/// <param name="AcknowledgedBy">Who acknowledged it; none while nobody has.</param>
/// <param name="AcknowledgedAt">When it was acknowledged.</param>
/// <param name="ResolvedBy">Who resolved it; none while it is open.</param>
/// <param name="ResolvedAt">When it was resolved.</param>
/// <param name="Subject">The one-line subject its notifications carry.</param>
/// <param name="Message">What the alert says: the body its notifications carry.</param>
public sealed record EscalationAlert(
    Guid Id, Guid TenantId, string SubjectKind, string SubjectName, string ThresholdName, string PolicyName, string State, int CurrentLevel,
    DateTimeOffset LevelReachedAt, DateTimeOffset FiredAt, string? AcknowledgedBy, DateTimeOffset? AcknowledgedAt, string? ResolvedBy, DateTimeOffset? ResolvedAt,
    string Subject, string Message);
