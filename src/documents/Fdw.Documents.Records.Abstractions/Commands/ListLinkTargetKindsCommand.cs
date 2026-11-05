using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Names the target kinds the connection can check for an organization (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListLinkTargetKinds", RestrictToCurrentCompilation = true)]
public sealed class ListLinkTargetKindsCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<string>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListLinkTargetKindsCommand()
        : base("ListLinkTargetKinds")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListLinkTargetKindsCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    public ListLinkTargetKindsCommand(Guid organizationId)
        : base("ListLinkTargetKinds")
    {
        OrganizationId = organizationId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId));
}
