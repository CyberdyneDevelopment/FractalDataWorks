using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Deletes a document. Answers whether one was there to delete.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "Delete", RestrictToCurrentCompilation = true)]
public sealed class DeleteCommand : DocumentCommandBase<bool>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public DeleteCommand()
        : base("Delete")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DeleteCommand"/> class.</summary>
    /// <param name="path">the document's path, relative to the scope</param>
    public DeleteCommand(DocumentPath path)
        : base("Delete")
    {

        Path = path;
    }

    /// <summary>Gets the document's path, relative to the scope.</summary>
    public DocumentPath Path { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        NotADocument(Path, "path");
}
