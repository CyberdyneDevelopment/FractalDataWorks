using System;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// The record of one document of an organization (FDW-3395): what it is and where it stands. Its files are its <see cref="IStoredMedia"/>.
/// </summary>
/// <remarks>
/// <para>
/// The application implements it over its own storage; FDW names no table. <see cref="Id"/> is a public UUIDv7 the application sets and
/// checks with <see cref="PublicIds.Check"/>; the application's own row id never leaves its implementation.
/// </para>
/// <para>
/// There is no held flag on purpose: legal hold is the platform's (<c>ILegalHolds</c>, FDW-1575), keyed by this id, so a document is held
/// when the platform says so and nowhere else.
/// </para>
/// </remarks>
public interface IDocumentRecord
{
    /// <summary>Gets the document's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the organization the document belongs to.</summary>
    Guid OrganizationId { get; }

    /// <summary>Gets the document type, as the organization's document configuration names it; it also selects the retention policy.</summary>
    string DocumentType { get; }

    /// <summary>Gets the document's title.</summary>
    string Title { get; }

    /// <summary>Gets the workflow stage the document is in, as the document configuration names it; its history is <see cref="IStateChange"/>.</summary>
    string State { get; }

    /// <summary>Gets the date the document takes effect, or null when it has none; retention may count from it.</summary>
    DateTimeOffset? EffectiveDate { get; }

    /// <summary>Gets when the document was uploaded.</summary>
    DateTimeOffset UploadedAt { get; }

    /// <summary>Gets the public id of the record node the document is attached to, or null when it is attached to none.</summary>
    Guid? RecordId { get; }
}
