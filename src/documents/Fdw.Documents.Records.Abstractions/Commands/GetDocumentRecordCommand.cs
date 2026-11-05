using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Reads one document of an organization by its public id (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "GetDocumentRecord", RestrictToCurrentCompilation = true)]
public sealed class GetDocumentRecordCommand : DocumentRecordCommandBase<IDocumentRecord>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public GetDocumentRecordCommand()
        : base("GetDocumentRecord")
    {
        OrganizationId = Guid.Empty;
        Id = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="GetDocumentRecordCommand"/> class.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <param name="id">The document's public id.</param>
    public GetDocumentRecordCommand(Guid organizationId, Guid id)
        : base("GetDocumentRecord")
    {
        OrganizationId = organizationId;
        Id = id;
    }

    /// <summary>Gets the organization.</summary>
    public Guid OrganizationId { get; }

    /// <summary>Gets the document's public id.</summary>
    public Guid Id { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(OrganizationProblem(OrganizationId), IdProblem(Id, "Document"));
}
