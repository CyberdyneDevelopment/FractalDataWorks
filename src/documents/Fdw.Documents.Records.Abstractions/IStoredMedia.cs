using System;
using Fdw.Documents.Abstractions;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// One file of a document (FDW-3395): its content, a rendition or an attachment, kept in the organization's document store.
/// </summary>
/// <remarks>
/// The record holds where the file is relative to the store, never an absolute location. <see cref="StoredUnder"/> is the assignment the
/// store was resolved through when the file was written (<c>IDocumentStore.StoredUnder</c>, FDW-3396); read the file through
/// <c>IDocumentStores.Stored(organization, StoredUnder)</c>, so it still resolves after the organization's storage moves.
/// </remarks>
public interface IStoredMedia
{
    /// <summary>Gets the file's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the public id of the document the file belongs to.</summary>
    Guid DocumentId { get; }

    /// <summary>Gets the file type, as the document configuration names it (for example Original or Rendition).</summary>
    string FileType { get; }

    /// <summary>Gets the size in bytes.</summary>
    long Length { get; }

    /// <summary>Gets the hash of the content.</summary>
    string ContentHash { get; }

    /// <summary>Gets what the store is for (for example Documents), as <c>IDocumentStores.For</c> was asked.</summary>
    string Purpose { get; }

    /// <summary>Gets the file's path, relative to the root of the store it was written to.</summary>
    DocumentPath Path { get; }

    /// <summary>Gets the id of the assignment the store was resolved through when the file was written (FDW-3396).</summary>
    Guid StoredUnder { get; }
}
