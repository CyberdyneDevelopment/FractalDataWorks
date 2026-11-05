using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Says whether the entity a link points at is there for the organization (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "LinkTargetExists", RestrictToCurrentCompilation = true)]
public sealed class LinkTargetExistsCommand : DocumentRecordCommandBase<bool>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public LinkTargetExistsCommand()
        : base("LinkTargetExists")
    {
        OrganizationId = Guid.Empty;
        TargetKind = string.Empty;
        TargetId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="LinkTargetExistsCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="targetKind">The target kind.</param>
    /// <param name="targetId">The target's public id.</param>
    public LinkTargetExistsCommand(Guid organizationId, string targetKind, Guid targetId)
        : base("LinkTargetExists")
    {
        OrganizationId = organizationId;
        TargetKind = targetKind;
        TargetId = targetId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the target kind.</summary>
    public string TargetKind { get; }

    /// <summary>Gets the target's public id.</summary>
    public Guid TargetId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), string.IsNullOrWhiteSpace(TargetKind) ? "the target kind is not named" : null);
}
