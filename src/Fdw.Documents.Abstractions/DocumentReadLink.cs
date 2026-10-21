using System;

namespace Fdw.Documents.Abstractions;

/// <summary>A temporary link a document can be read from without the connection's credentials (FDW-1497).</summary>
public sealed class DocumentReadLink
{
    /// <summary>Initializes a new instance of the <see cref="DocumentReadLink"/> class.</summary>
    /// <param name="link">The link.</param>
    /// <param name="expiresAt">When it stops working.</param>
    public DocumentReadLink(Uri link, DateTimeOffset expiresAt)
    {
        Link = link ?? throw new ArgumentNullException(nameof(link));
        ExpiresAt = expiresAt;
    }

    /// <summary>Gets the link.</summary>
    public Uri Link { get; }

    /// <summary>Gets when it stops working.</summary>
    public DateTimeOffset ExpiresAt { get; }
}
