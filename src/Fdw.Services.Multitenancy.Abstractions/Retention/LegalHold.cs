using System;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// A legal hold on one document of an organization (FDW-1575). While it stands the retention job does not end the document; lifting it keeps
/// the record of who held the document and who released it.
/// </summary>
/// <param name="Id">The hold's identifier.</param>
/// <param name="OrganizationId">The organization that holds the document.</param>
/// <param name="DocumentId">The identifier the application gives the document; the platform does not interpret it.</param>
/// <param name="Reason">Why the document is held.</param>
/// <param name="PlacedBy">The user who placed the hold.</param>
/// <param name="PlacedAt">When the hold was placed.</param>
/// <param name="LiftedBy">The user who lifted the hold, or null while it stands.</param>
/// <param name="LiftedAt">When the hold was lifted, or null while it stands.</param>
public sealed record LegalHold(Guid Id, Guid OrganizationId, string DocumentId, string Reason, string PlacedBy, DateTimeOffset PlacedAt, string? LiftedBy, DateTimeOffset? LiftedAt)
{
    /// <summary>Gets a value indicating whether the hold still stands.</summary>
    public bool IsHeld => LiftedAt is null;
}
