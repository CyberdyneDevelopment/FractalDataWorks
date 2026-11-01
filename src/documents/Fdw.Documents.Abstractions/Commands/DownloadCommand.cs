using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Reads a document as a stream, whole or a byte range of it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "Download", RestrictToCurrentCompilation = true)]
public sealed class DownloadCommand : DocumentCommandBase<DocumentContent>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public DownloadCommand()
        : base("Download")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DownloadCommand"/> class.</summary>
    /// <param name="path">the document's path, relative to the scope</param>
    /// <param name="range">the byte range to read; null reads the whole document</param>
    public DownloadCommand(DocumentPath path, DocumentRange? range)
        : base("Download")
    {

        Path = path;
        Range = range;
    }

    /// <summary>Gets the document's path, relative to the scope.</summary>
    public DocumentPath Path { get; }

    /// <summary>Gets the byte range to read; null reads the whole document.</summary>
    public DocumentRange? Range { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(NotADocument(Path, "path"), Range is { IsValid: false } ? "a range starts at zero or later and holds at least one byte" : null);
}
