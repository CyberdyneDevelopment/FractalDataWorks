using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Lists the children of a record node, or the nodes at the top of the tree (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListRecordChildren", RestrictToCurrentCompilation = true)]
public sealed class ListRecordChildrenCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<IRecordNode>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListRecordChildrenCommand()
        : base("ListRecordChildren")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListRecordChildrenCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="parentId">The parent's public id, or null for the top of the tree.</param>
    public ListRecordChildrenCommand(Guid organizationId, Guid? parentId)
        : base("ListRecordChildren")
    {
        OrganizationId = organizationId;
        ParentId = parentId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the parent's public id, or null for the top of the tree.</summary>
    public Guid? ParentId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId));
}
