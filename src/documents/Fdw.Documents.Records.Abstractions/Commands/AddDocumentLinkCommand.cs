using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Adds a link that FDW's link rule passed, and raises the revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "AddDocumentLink", RestrictToCurrentCompilation = true)]
public sealed class AddDocumentLinkCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public AddDocumentLinkCommand()
        : base("AddDocumentLink")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="AddDocumentLinkCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="link">The link.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public AddDocumentLinkCommand(Guid organizationId, Links.IDocumentLink? link, long expectedRevision)
        : base("AddDocumentLink")
    {
        OrganizationId = organizationId;
        Link = link;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the link.</summary>
    public Links.IDocumentLink? Link { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Link, entity => entity.Id, "DocumentLink"), RevisionProblem(ExpectedRevision));
}
