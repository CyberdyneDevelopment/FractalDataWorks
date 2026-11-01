using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Copies a document inside the storage. Answers the copy.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "Copy", RestrictToCurrentCompilation = true)]
public sealed class CopyCommand : DocumentCommandBase<DocumentInfo>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public CopyCommand()
        : base("Copy")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="CopyCommand"/> class.</summary>
    /// <param name="source">the document to copy</param>
    /// <param name="destination">where the copy goes</param>
    /// <param name="createOnly">whether the copy is refused when a document already exists at the destination</param>
    public CopyCommand(DocumentPath source, DocumentPath destination, bool createOnly)
        : base("Copy")
    {

        Source = source;
        Destination = destination;
        CreateOnly = createOnly;
    }

    /// <summary>Gets the document to copy.</summary>
    public DocumentPath Source { get; }

    /// <summary>Gets where the copy goes.</summary>
    public DocumentPath Destination { get; }

    /// <summary>Gets whether the copy is refused when a document already exists at the destination.</summary>
    public bool CreateOnly { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(NotADocument(Source, "source"), NotADocument(Destination, "destination"), Source == Destination ? "the source and the destination are the same path" : null);
}
