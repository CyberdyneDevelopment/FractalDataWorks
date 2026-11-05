using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Adds the record of a file written to the organization's document store, and raises the revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "AddStoredMedia", RestrictToCurrentCompilation = true)]
public sealed class AddStoredMediaCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public AddStoredMediaCommand()
        : base("AddStoredMedia")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="AddStoredMediaCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="media">The file.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public AddStoredMediaCommand(Guid organizationId, IStoredMedia? media, long expectedRevision)
        : base("AddStoredMedia")
    {
        OrganizationId = organizationId;
        Media = media;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the file.</summary>
    public IStoredMedia? Media { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Media, entity => entity.Id, "StoredMedia"), RevisionProblem(ExpectedRevision));
}
