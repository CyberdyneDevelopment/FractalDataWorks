using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Lists the attribute values of one document or record node (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ListAttributeValues", RestrictToCurrentCompilation = true)]
public sealed class ListAttributeValuesCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<IAttributeValue>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ListAttributeValuesCommand()
        : base("ListAttributeValues")
    {
        OrganizationId = Guid.Empty;
        OwnerId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ListAttributeValuesCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="ownerId">The public id of the document or node.</param>
    public ListAttributeValuesCommand(Guid organizationId, Guid ownerId)
        : base("ListAttributeValues")
    {
        OrganizationId = organizationId;
        OwnerId = ownerId;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the public id of the document or node.</summary>
    public Guid OwnerId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), IdProblem(OwnerId, "Owner"));
}
