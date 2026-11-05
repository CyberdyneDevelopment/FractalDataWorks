using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Lists a record node's requirements (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListRecordRequirements", RestrictToCurrentCompilation = true)]
public sealed class ListRecordRequirementsCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<IRecordRequirement>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListRecordRequirementsCommand()
        : base("ListRecordRequirements")
    {
        OrganizationId = Guid.Empty;
        RecordId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListRecordRequirementsCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="recordId">The node's public id.</param>
    public ListRecordRequirementsCommand(Guid organizationId, Guid recordId)
        : base("ListRecordRequirements")
    {
        OrganizationId = organizationId;
        RecordId = recordId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the node's public id.</summary>
    public Guid RecordId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), IdProblem(RecordId, "RecordNode"));
}
