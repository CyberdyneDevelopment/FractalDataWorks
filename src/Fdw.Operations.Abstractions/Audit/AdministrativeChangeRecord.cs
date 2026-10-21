using System;

namespace Fdw.Operations.Abstractions.Audit;

/// <summary>
/// One recorded administrative change, as it is read back: who, what, where, old to new, and when. Read only.
/// </summary>
public sealed class AdministrativeChangeRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrativeChangeRecord"/> class.
    /// </summary>
    /// <param name="id">The record's identity.</param>
    /// <param name="occurredAt">When the change was recorded.</param>
    /// <param name="actorUserId">Who made the change.</param>
    /// <param name="actorName">The name they had then.</param>
    /// <param name="changeKind">What was done.</param>
    /// <param name="itemKind">What it was done to.</param>
    /// <param name="itemId">Which one.</param>
    /// <param name="level">The name of the level it was made at.</param>
    /// <param name="tenantId">The tenant it was made in.</param>
    /// <param name="organizationId">The organization it was made in, when at the Organization level.</param>
    /// <param name="oldValue">The value before.</param>
    /// <param name="newValue">The value after.</param>
    /// <param name="reason">Why, when a reason was given.</param>
    public AdministrativeChangeRecord(
        Guid id,
        DateTimeOffset occurredAt,
        Guid actorUserId,
        string actorName,
        string changeKind,
        string itemKind,
        string itemId,
        string level,
        Guid tenantId,
        Guid? organizationId,
        string? oldValue,
        string? newValue,
        string? reason)
    {
        Id = id;
        OccurredAt = occurredAt;
        ActorUserId = actorUserId;
        ActorName = actorName;
        ChangeKind = changeKind;
        ItemKind = itemKind;
        ItemId = itemId;
        Level = level;
        TenantId = tenantId;
        OrganizationId = organizationId;
        OldValue = oldValue;
        NewValue = newValue;
        Reason = reason;
    }

    /// <summary>Gets the record's identity.</summary>
    public Guid Id { get; }

    /// <summary>Gets when the change was recorded.</summary>
    public DateTimeOffset OccurredAt { get; }

    /// <summary>Gets who made the change.</summary>
    public Guid ActorUserId { get; }

    /// <summary>Gets the name they had then.</summary>
    public string ActorName { get; }

    /// <summary>Gets what was done.</summary>
    public string ChangeKind { get; }

    /// <summary>Gets what it was done to.</summary>
    public string ItemKind { get; }

    /// <summary>Gets which one.</summary>
    public string ItemId { get; }

    /// <summary>Gets the name of the level it was made at.</summary>
    public string Level { get; }

    /// <summary>Gets the tenant it was made in.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the organization it was made in.</summary>
    public Guid? OrganizationId { get; }

    /// <summary>Gets the value before.</summary>
    public string? OldValue { get; }

    /// <summary>Gets the value after.</summary>
    public string? NewValue { get; }

    /// <summary>Gets why, when a reason was given.</summary>
    public string? Reason { get; }
}
