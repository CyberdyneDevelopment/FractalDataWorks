using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Reads one record node of an organization by its public id (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "GetRecordNode", RestrictToCurrentCompilation = true)]
public sealed class GetRecordNodeCommand : DocumentRecordCommandBase<IRecordNode>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public GetRecordNodeCommand()
        : base("GetRecordNode")
    {
        OrganizationId = Guid.Empty;
        Id = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="GetRecordNodeCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The node's public id.</param>
    public GetRecordNodeCommand(Guid organizationId, Guid id)
        : base("GetRecordNode")
    {
        OrganizationId = organizationId;
        Id = id;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the node's public id.</summary>
    public Guid Id { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), IdProblem(Id, "RecordNode"));
}
