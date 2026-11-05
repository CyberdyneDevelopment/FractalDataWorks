using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Saves a document, new or changed, and raises the organization's revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "SaveDocumentRecord", RestrictToCurrentCompilation = true)]
public sealed class SaveDocumentRecordCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public SaveDocumentRecordCommand()
        : base("SaveDocumentRecord")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="SaveDocumentRecordCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="document">The document.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public SaveDocumentRecordCommand(Guid organizationId, IDocumentRecord? document, long expectedRevision)
        : base("SaveDocumentRecord")
    {
        OrganizationId = organizationId;
        Document = document;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the document.</summary>
    public IDocumentRecord? Document { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Document, entity => entity.Id, "Document"), RevisionProblem(ExpectedRevision));
}
