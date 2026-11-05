using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Operations.Data;

/// <summary>
/// One row of ops.EscalationAlert (FDW-3863): an alert an escalation policy is walking. Property names match the column names exactly.
/// </summary>
/// <remarks>The table has no defaults: <see cref="TenantId"/> and every state value are stated by the code that writes the row.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class EscalationAlertRow
{
    /// <summary>Gets or sets the alert's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the tenant the threshold belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets what kind of owner fired: Server, Pipeline, Schedule or Component.</summary>
    public string SubjectKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner's name.</summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>Gets or sets the threshold's name.</summary>
    public string ThresholdName { get; set; } = string.Empty;

    /// <summary>Gets or sets the escalation policy the alert follows, by the name it had when the alert fired.</summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>Gets or sets the alert's state: Firing, Acknowledged or Resolved.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the highest level sent so far; 0 when none has been.</summary>
    public int CurrentLevel { get; set; }

    /// <summary>Gets or sets when that level was sent; when the alert fired while none has been.</summary>
    public DateTimeOffset LevelReachedAt { get; set; }

    /// <summary>Gets or sets when the threshold fired.</summary>
    public DateTimeOffset FiredAt { get; set; }

    /// <summary>Gets or sets who acknowledged the alert.</summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>Gets or sets when it was acknowledged.</summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }

    /// <summary>Gets or sets who resolved the alert.</summary>
    public string? ResolvedBy { get; set; }

    /// <summary>Gets or sets when it was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Gets or sets the one-line subject every level's notification carries.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets or sets the body every level's notification carries.</summary>
    public string Message { get; set; } = string.Empty;
}
