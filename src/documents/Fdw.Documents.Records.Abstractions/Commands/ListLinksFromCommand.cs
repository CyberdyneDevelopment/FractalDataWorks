using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Lists the links that start from a document (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListLinksFrom", RestrictToCurrentCompilation = true)]
public sealed class ListLinksFromCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<Links.IDocumentLink>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListLinksFromCommand()
        : base("ListLinksFrom")
    {
        OrganizationId = Guid.Empty;
        DocumentId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListLinksFromCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentId">The document's public id.</param>
    public ListLinksFromCommand(Guid organizationId, Guid documentId)
        : base("ListLinksFrom")
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
