using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Lists the documents in a folder, a page at a time.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "List", RestrictToCurrentCompilation = true)]
public sealed class ListCommand : DocumentCommandBase<DocumentPage>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListCommand()
        : base("List")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ListCommand"/> class.</summary>
    /// <param name="folder">the folder to list; the root lists the scope</param>
    /// <param name="recursive">whether documents in folders below it are listed too</param>
    /// <param name="pageSize">how many documents a page holds at most; at least one</param>
    /// <param name="continuation">what the previous page gave back; null starts from the first</param>
    public ListCommand(DocumentPath folder, bool recursive, int pageSize, string? continuation)
        : base("List")
    {

        Folder = folder;
        Recursive = recursive;
        PageSize = pageSize;
        Continuation = continuation;
    }

    /// <summary>Gets the folder to list; the root lists the scope.</summary>
    public DocumentPath Folder { get; }

    /// <summary>Gets whether documents in folders below it are listed too.</summary>
    public bool Recursive { get; }

    /// <summary>Gets how many documents a page holds at most; at least one.</summary>
    public int PageSize { get; }

    /// <summary>Gets what the previous page gave back; null starts from the first.</summary>
    public string? Continuation { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        PageSize < 1 ? "a page holds at least one document" : null;
}
