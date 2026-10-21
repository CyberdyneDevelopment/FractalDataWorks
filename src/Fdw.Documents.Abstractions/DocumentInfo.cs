using System;
using System.Collections.Generic;

namespace Fdw.Documents.Abstractions;

/// <summary>What a storage reports about a document (FDW-1497).</summary>
public sealed class DocumentInfo
{
    /// <summary>Initializes a new instance of the <see cref="DocumentInfo"/> class.</summary>
    /// <param name="path">The document's path, relative to the scope.</param>
    /// <param name="length">The size in bytes.</param>
    /// <param name="contentType">The content type the storage holds; null when it holds none.</param>
    /// <param name="lastModified">When the content last changed.</param>
    /// <param name="contentHash">A hash of the content the storage computed; null when the connection does not declare ContentHash.</param>
    /// <param name="version">The storage's own version of the document; null when the connection does not declare Versioning.</param>
    /// <param name="metadata">The named metadata kept with the document; empty when the connection does not declare Metadata.</param>
    public DocumentInfo(
        DocumentPath path,
        long length,
        string? contentType,
        DateTimeOffset lastModified,
        string? contentHash,
        string? version,
        IReadOnlyDictionary<string, string> metadata)
    {
        Path = path;
        Length = length;
        ContentType = contentType;
        LastModified = lastModified;
        ContentHash = contentHash;
        Version = version;
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>Gets the document's path, relative to the scope.</summary>
    public DocumentPath Path { get; }

    /// <summary>Gets the size in bytes.</summary>
    public long Length { get; }

    /// <summary>Gets the content type the storage holds, or null.</summary>
    public string? ContentType { get; }

    /// <summary>Gets when the content last changed.</summary>
    public DateTimeOffset LastModified { get; }

    /// <summary>Gets a hash of the content the storage computed, or null.</summary>
    public string? ContentHash { get; }

    /// <summary>Gets the storage's own version of the document, or null.</summary>
    public string? Version { get; }

    /// <summary>Gets the named metadata kept with the document.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }
}
