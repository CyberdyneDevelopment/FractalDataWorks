using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Operations.Data;

/// <summary>
/// One administrative change, as it is stored in the ops.AdministrativeChange table. The table is append only.
/// </summary>
/// <remarks>
/// Property names match the column names exactly. <see cref="TenantId"/> is required and has no default: the service filters
/// every read by it, so a row without one would belong to nobody.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed class AdministrativeChange
{
    /// <summary>Gets or sets the identity of the record.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets when the change was recorded.</summary>
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets who made the change.</summary>
    public Guid ActorUserId { get; set; }

    /// <summary>Gets or sets the name the person had then.</summary>
    public string ActorName { get; set; } = string.Empty;

    /// <summary>Gets or sets what was done.</summary>
    public string ChangeKind { get; set; } = string.Empty;

    /// <summary>Gets or sets what it was done to.</summary>
    public string ItemKind { get; set; } = string.Empty;

    /// <summary>Gets or sets which one.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the level the change was made at.</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant the change was made in.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the organization the change was made in, when it was made at the Organization level.</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Gets or sets the value before.</summary>
    public string? OldValue { get; set; }

    /// <summary>Gets or sets the value after.</summary>
    public string? NewValue { get; set; }

    /// <summary>Gets or sets why, when a reason was given.</summary>
    public string? Reason { get; set; }
}
