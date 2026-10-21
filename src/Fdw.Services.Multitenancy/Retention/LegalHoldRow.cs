using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Multitenancy;

/// <summary>
/// A row of the table of legal holds (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class LegalHoldRow
{
    /// <summary>Gets or sets the row's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the tenant the organization belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the organization that holds the document.</summary>
    public Guid OrganizationId { get; set; }

    /// <summary>Gets or sets the application's identifier of the document.</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Gets or sets why the document is held.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who placed the hold.</summary>
    public string PlacedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets when the hold was placed.</summary>
    public DateTimeOffset PlacedAt { get; set; }

    /// <summary>Gets or sets the user who lifted the hold, or null while it stands.</summary>
    public string? LiftedBy { get; set; }

    /// <summary>Gets or sets when the hold was lifted, or null while it stands.</summary>
    public DateTimeOffset? LiftedAt { get; set; }
}
