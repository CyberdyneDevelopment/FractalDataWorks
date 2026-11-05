using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Starts the organization's document records at their first revision; refused when already started (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "StartOrganizationRevision", RestrictToCurrentCompilation = true)]
public sealed class StartOrganizationRevisionCommand : DocumentRecordCommandBase<IOrganizationRevision>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public StartOrganizationRevisionCommand()
        : base("StartOrganizationRevision")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="StartOrganizationRevisionCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    public StartOrganizationRevisionCommand(Guid organizationId)
        : base("StartOrganizationRevision")
    {
        OrganizationId = organizationId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId));
}
