using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Sets one attribute value, replacing the owner's value of the same code, and raises the revision (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "SetAttributeValue", RestrictToCurrentCompilation = true)]
public sealed class SetAttributeValueCommand : DocumentRecordCommandBase<long>, IRevisedDocumentRecordCommand
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public SetAttributeValueCommand()
        : base("SetAttributeValue")
    {
        OrganizationId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="SetAttributeValueCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="value">The value.</param>
    /// <param name="expectedRevision">The organization's revision the caller read.</param>
    public SetAttributeValueCommand(Guid organizationId, IAttributeValue? value, long expectedRevision)
        : base("SetAttributeValue")
    {
        OrganizationId = organizationId;
        Value = value;
        ExpectedRevision = expectedRevision;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the value.</summary>
    public IAttributeValue? Value { get; }

    /// <summary>Gets the organization's revision the caller read.</summary>
    public long ExpectedRevision { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), EntityProblem(Value, entity => entity.Id, "AttributeValue"), RevisionProblem(ExpectedRevision));
}
