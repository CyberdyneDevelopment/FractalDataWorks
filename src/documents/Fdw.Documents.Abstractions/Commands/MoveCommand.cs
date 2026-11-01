using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Moves a document inside the storage. Answers it at its new path.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "Move", RestrictToCurrentCompilation = true)]
public sealed class MoveCommand : DocumentCommandBase<DocumentInfo>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public MoveCommand()
        : base("Move")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="MoveCommand"/> class.</summary>
    /// <param name="source">the document to move</param>
    /// <param name="destination">where it goes</param>
    /// <param name="createOnly">whether the move is refused when a document already exists at the destination</param>
    public MoveCommand(DocumentPath source, DocumentPath destination, bool createOnly)
        : base("Move")
    {

        Source = source;
        Destination = destination;
        CreateOnly = createOnly;
    }

    /// <summary>Gets the document to move.</summary>
    public DocumentPath Source { get; }

    /// <summary>Gets where it goes.</summary>
    public DocumentPath Destination { get; }

    /// <summary>Gets whether the move is refused when a document already exists at the destination.</summary>
    public bool CreateOnly { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(NotADocument(Source, "source"), NotADocument(Destination, "destination"), Source == Destination ? "the source and the destination are the same path" : null);
}
