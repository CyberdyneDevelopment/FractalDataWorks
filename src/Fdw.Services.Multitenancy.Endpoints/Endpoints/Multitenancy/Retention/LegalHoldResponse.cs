using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// A legal hold as the endpoints return it (FDW-1575).
/// </summary>
public class LegalHoldResponse
{
    /// <summary>Gets or sets the hold's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the application's identifier of the held document.</summary>
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

    /// <summary>Gets or sets a value indicating whether the hold still stands.</summary>
    public bool IsHeld { get; set; }
}
