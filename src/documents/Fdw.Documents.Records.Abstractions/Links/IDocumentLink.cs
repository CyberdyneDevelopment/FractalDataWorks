using System;

namespace Fdw.Documents.Records.Abstractions.Links;

/// <summary>
/// A link from a document to another document, a record node or another entity (FDW-3397), by link kind and the target's public id.
/// </summary>
/// <remarks>
/// A foreign key cannot reach several tables, so the target is checked when the link is added (<see cref="DocumentLinkRule"/>), not by
/// the database: an unknown kind or a target that is not there is refused, never stored.
/// </remarks>
public interface IDocumentLink
{
    /// <summary>Gets the link's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the organization the link belongs to.</summary>
    Guid OrganizationId { get; }

    /// <summary>Gets the public id of the document the link starts from.</summary>
    Guid DocumentId { get; }

    /// <summary>Gets the link kind (for example replaces or supersedes), as the organization's link kind definitions name it.</summary>
    string LinkKind { get; }

    /// <summary>Gets the kind of entity the link points at, one of those <see cref="ILinkTargets.Kinds"/> names.</summary>
    string TargetKind { get; }

    /// <summary>Gets the public id of the entity the link points at.</summary>
    Guid TargetId { get; }
}
