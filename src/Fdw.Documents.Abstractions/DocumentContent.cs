using System;
using System.IO;

namespace Fdw.Documents.Abstractions;

/// <summary>A document read from the storage: what is known of it and its content as a stream the caller disposes (FDW-1497).</summary>
public sealed class DocumentContent : IDisposable
{
    /// <summary>Initializes a new instance of the <see cref="DocumentContent"/> class.</summary>
    /// <param name="info">What the storage reports about the document.</param>
    /// <param name="content">The content, positioned at its start; for a ranged read, at the start of the range.</param>
    public DocumentContent(DocumentInfo info, Stream content)
    {
        Info = info ?? throw new ArgumentNullException(nameof(info));
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>Gets what the storage reports about the document.</summary>
    public DocumentInfo Info { get; }

    /// <summary>Gets the content.</summary>
    public Stream Content { get; }

    /// <inheritdoc />
    public void Dispose() => Content.Dispose();
}
