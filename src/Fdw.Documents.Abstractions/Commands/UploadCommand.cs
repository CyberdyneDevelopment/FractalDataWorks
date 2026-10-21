using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>Stores a document: the content is streamed, never held whole. A create-only write is refused when a document is already there.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentCommands), "Upload", RestrictToCurrentCompilation = true)]
public sealed class UploadCommand : DocumentCommandBase<DocumentInfo>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public UploadCommand()
        : base("Upload")
    {
        Content = Stream.Null;
        Metadata = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>Initializes a new instance of the <see cref="UploadCommand"/> class.</summary>
    /// <param name="path">the document's path, relative to the scope</param>
    /// <param name="content">the content to store, read from its current position</param>
    /// <param name="contentType">the content type to keep with it; null states none</param>
    /// <param name="createOnly">whether the write is refused when a document already exists at the path (FDW-1499 CreateOnlyWrites)</param>
    /// <param name="metadata">the named metadata to keep with it; empty states none</param>
    public UploadCommand(DocumentPath path, Stream content, string? contentType, bool createOnly, IReadOnlyDictionary<string, string> metadata)
        : base("Upload")
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        Path = path;
        Content = content;
        ContentType = contentType;
        CreateOnly = createOnly;
        Metadata = metadata;
    }

    /// <summary>Gets the document's path, relative to the scope.</summary>
    public DocumentPath Path { get; }

    /// <summary>Gets the content to store, read from its current position.</summary>
    public Stream Content { get; }

    /// <summary>Gets the content type to keep with it; null states none.</summary>
    public string? ContentType { get; }

    /// <summary>Gets whether the write is refused when a document already exists at the path (FDW-1499 CreateOnlyWrites).</summary>
    public bool CreateOnly { get; }

    /// <summary>Gets the named metadata to keep with it; empty states none.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(NotADocument(Path, "path"));
}
