using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Reads the organization's current revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "GetOrganizationRevision", RestrictToCurrentCompilation = true)]
public sealed class GetOrganizationRevisionCommand : DocumentRecordCommandBase<IOrganizationRevision>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public GetOrganizationRevisionCommand()
        : base("GetOrganizationRevision")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="GetOrganizationRevisionCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    public GetOrganizationRevisionCommand(Guid organizationId)
        : base("GetOrganizationRevision")
    {
        OrganizationId = organizationId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId));
}
