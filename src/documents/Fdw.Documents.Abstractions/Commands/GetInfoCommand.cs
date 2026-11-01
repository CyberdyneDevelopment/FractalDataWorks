using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Asks what the storage reports about a document, without its content.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "GetInfo", RestrictToCurrentCompilation = true)]
public sealed class GetInfoCommand : DocumentCommandBase<DocumentInfo>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public GetInfoCommand()
        : base("GetInfo")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GetInfoCommand"/> class.</summary>
    /// <param name="path">the document's path, relative to the scope</param>
    public GetInfoCommand(DocumentPath path)
        : base("GetInfo")
    {

        Path = path;
    }

    /// <summary>Gets the document's path, relative to the scope.</summary>
    public DocumentPath Path { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        NotADocument(Path, "path");
}
