using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Creates a temporary link a document can be read from. Refused by a connection that does not declare ReadLinks.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "CreateReadLink", RestrictToCurrentCompilation = true)]
public sealed class CreateReadLinkCommand : DocumentCommandBase<DocumentReadLink>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public CreateReadLinkCommand()
        : base("CreateReadLink")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="CreateReadLinkCommand"/> class.</summary>
    /// <param name="path">the document's path, relative to the scope</param>
    /// <param name="lifetime">how long the link works; longer than zero</param>
    public CreateReadLinkCommand(DocumentPath path, TimeSpan lifetime)
        : base("CreateReadLink")
    {

        Path = path;
        Lifetime = lifetime;
    }

    /// <summary>Gets the document's path, relative to the scope.</summary>
    public DocumentPath Path { get; }

    /// <summary>Gets how long the link works; longer than zero.</summary>
    public TimeSpan Lifetime { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(NotADocument(Path, "path"), Lifetime <= TimeSpan.Zero ? "a read link lives longer than zero" : null);
}
