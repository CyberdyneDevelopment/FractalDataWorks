using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Moves a document into a state, recording the change, and raises the revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "EnterState", RestrictToCurrentCompilation = true)]
public sealed class EnterStateCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public EnterStateCommand()
        : base("EnterState")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="EnterStateCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="change">The change.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public EnterStateCommand(Guid organizationId, IStateChange? change, long expectedRevision)
        : base("EnterState")
    {
        OrganizationId = organizationId;
        Change = change;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the change.</summary>
    public IStateChange? Change { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Change, entity => entity.Id, "StateChange"), RevisionProblem(ExpectedRevision));
}
