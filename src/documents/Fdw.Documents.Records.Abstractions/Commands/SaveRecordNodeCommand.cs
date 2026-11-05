using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Saves a record node, new or changed, and raises the revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "SaveRecordNode", RestrictToCurrentCompilation = true)]
public sealed class SaveRecordNodeCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public SaveRecordNodeCommand()
        : base("SaveRecordNode")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="SaveRecordNodeCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="node">The node.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public SaveRecordNodeCommand(Guid organizationId, IRecordNode? node, long expectedRevision)
        : base("SaveRecordNode")
    {
        OrganizationId = organizationId;
        Node = node;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the node.</summary>
    public IRecordNode? Node { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Node, entity => entity.Id, "RecordNode"), RevisionProblem(ExpectedRevision));
}
