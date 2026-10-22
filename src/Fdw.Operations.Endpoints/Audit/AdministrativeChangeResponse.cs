using System;
using Fdw.Operations.Abstractions.Audit;

namespace Fdw.Operations.Endpoints.Audit;

/// <summary>
/// One administrative change as the route answers it.
/// </summary>
public class AdministrativeChangeResponse
{
    /// <summary>Gets or sets the identity of the record.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets when the change was recorded.</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>Gets or sets who made the change.</summary>
    public Guid ActorUserId { get; set; }

    /// <summary>Gets or sets the name the person had then.</summary>
    public required string ActorName { get; set; }

    /// <summary>Gets or sets what was done.</summary>
    public required string ChangeKind { get; set; }

    /// <summary>Gets or sets what it was done to.</summary>
    public required string ItemKind { get; set; }

    /// <summary>Gets or sets which one.</summary>
    public required string ItemId { get; set; }

    /// <summary>Gets or sets the level the change was made at.</summary>
    public required string Level { get; set; }

    /// <summary>Gets or sets the tenant the change was made in.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the organization the change was made in.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Gets or sets the value before.</summary>
    public string? OldValue { get; set; }

    /// <summary>Gets or sets the value after.</summary>
    public string? NewValue { get; set; }

    /// <summary>Gets or sets why, when a reason was given.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets the role the actor was acting with when the change was made, or null when they made it in their own session.</summary>
    public string? ActingRole { get; set; }

    /// <summary>
    /// Builds the response for a recorded change.
    /// </summary>
    /// <param name="record">The recorded change.</param>
    /// <returns>The response.</returns>
    public static AdministrativeChangeResponse Of(AdministrativeChangeRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new AdministrativeChangeResponse
        {
            Id = record.Id,
            OccurredAt = record.OccurredAt,
            ActorUserId = record.ActorUserId,
            ActorName = record.ActorName,
            ChangeKind = record.ChangeKind,
            ItemKind = record.ItemKind,
            ItemId = record.ItemId,
            Level = record.Level,
            TenantId = record.TenantId,
            OrganizationId = record.OrganizationId,
            OldValue = record.OldValue,
            NewValue = record.NewValue,
            Reason = record.Reason,
            ActingRole = record.ActingRole
        };
    }
}
