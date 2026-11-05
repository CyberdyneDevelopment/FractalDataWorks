using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Removes a link and raises the revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "RemoveDocumentLink", RestrictToCurrentCompilation = true)]
public sealed class RemoveDocumentLinkCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public RemoveDocumentLinkCommand()
        : base("RemoveDocumentLink")
    {
        OrganizationId = Guid.Empty;
        Id = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="RemoveDocumentLinkCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The link's public id.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public RemoveDocumentLinkCommand(Guid organizationId, Guid id, long expectedRevision)
        : base("RemoveDocumentLink")
    {
        OrganizationId = organizationId;
        Id = id;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the link's public id.</summary>
    public Guid Id { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), IdProblem(Id, "DocumentLink"), RevisionProblem(ExpectedRevision));
}
