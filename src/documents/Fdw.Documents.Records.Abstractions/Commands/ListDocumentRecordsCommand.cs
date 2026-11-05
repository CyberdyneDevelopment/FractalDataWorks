using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Lists an organization's documents of one type (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListDocumentRecords", RestrictToCurrentCompilation = true)]
public sealed class ListDocumentRecordsCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<IDocumentRecord>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListDocumentRecordsCommand()
        : base("ListDocumentRecords")
    {
        OrganizationId = Guid.Empty;
        DocumentType = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListDocumentRecordsCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="documentType">The document type.</param>
    public ListDocumentRecordsCommand(Guid organizationId, string documentType)
        : base("ListDocumentRecords")
    {
        OrganizationId = organizationId;
        DocumentType = documentType;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the document type.</summary>
    public string DocumentType { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), string.IsNullOrWhiteSpace(DocumentType) ? "the document type is not named" : null);
}
