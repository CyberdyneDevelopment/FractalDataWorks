using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Saves a requirement, new or satisfied, and raises the revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "SaveRecordRequirement", RestrictToCurrentCompilation = true)]
public sealed class SaveRecordRequirementCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public SaveRecordRequirementCommand()
        : base("SaveRecordRequirement")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="SaveRecordRequirementCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="requirement">The requirement.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public SaveRecordRequirementCommand(Guid organizationId, IRecordRequirement? requirement, long expectedRevision)
        : base("SaveRecordRequirement")
    {
        OrganizationId = organizationId;
        Requirement = requirement;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the requirement.</summary>
    public IRecordRequirement? Requirement { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Requirement, entity => entity.Id, "RecordRequirement"), RevisionProblem(ExpectedRevision));
}
