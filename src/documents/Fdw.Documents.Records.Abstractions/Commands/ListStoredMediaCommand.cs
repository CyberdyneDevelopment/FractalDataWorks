using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Lists a document's files (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListStoredMedia", RestrictToCurrentCompilation = true)]
public sealed class ListStoredMediaCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<IStoredMedia>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListStoredMediaCommand()
        : base("ListStoredMedia")
    {
        OrganizationId = Guid.Empty;
        DocumentId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListStoredMediaCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document's public id.</param>
    public ListStoredMediaCommand(Guid organizationId, Guid documentId)
        : base("ListStoredMedia")
    {
        OrganizationId = organizationId;
        DocumentId = documentId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the document's public id.</summary>
    public Guid DocumentId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), IdProblem(DocumentId, "Document"));
}
