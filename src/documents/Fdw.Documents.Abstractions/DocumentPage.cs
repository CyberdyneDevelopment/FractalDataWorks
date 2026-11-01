using System;
using System.Collections.Generic;

namespace Fdw.Documents.Abstractions;

/// <summary>One page of a listing (FDW-1497): the documents, and where the next page starts.</summary>
public sealed class DocumentPage
{
    /// <summary>Initializes a new instance of the <see cref="DocumentPage"/> class.</summary>
    /// <param name="items">The documents of this page, in the storage's order.</param>
    /// <param name="continuation">What to give back to read the next page; null when this is the last.</param>
    public DocumentPage(IReadOnlyList<DocumentInfo> items, string? continuation)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        Continuation = continuation;
    }

    /// <summary>Gets the documents of this page.</summary>
    public IReadOnlyList<DocumentInfo> Items { get; }

    /// <summary>Gets what to give back for the next page; null on the last.</summary>
    public string? Continuation { get; }
}
