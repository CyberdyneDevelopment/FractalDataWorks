using System;

namespace Fdw.Operations.Abstractions.Escalation;

/// <summary>What a threshold that fired asks of escalation (FDW-3863): to open an alert on a policy it names.</summary>
/// <param name="TenantId">The tenant the threshold belongs to.</param>
/// <param name="SubjectKind">What kind of owner fired: Server, Pipeline, Schedule or Component.</param>
/// <param name="SubjectName">The owner's name.</param>
/// <param name="ThresholdName">The threshold's name.</param>
/// <param name="PolicyName">The escalation policy to follow. Required: a threshold names its policy, none is assumed.</param>
/// <param name="Subject">The one-line subject every level's notification carries.</param>
/// <param name="Message">What the alert says: the body every level's notification carries. A caller that has detail lines (the last failures, one to a line) puts them here, one to a line.</param>
public sealed record EscalationAlertRequest(
    Guid TenantId, string SubjectKind, string SubjectName, string ThresholdName, string PolicyName, string Subject, string Message);
