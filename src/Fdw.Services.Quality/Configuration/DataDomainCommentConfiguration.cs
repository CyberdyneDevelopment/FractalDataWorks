using System;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Maps to <c>data.DataDomainComment</c> — comments on a data domain proposal.
/// Child of <see cref="DataDomainProposalConfiguration"/> via ProposalId FK.
/// </summary>
[GenerateMapper]
public sealed partial class DataDomainCommentConfiguration
{

    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the comment record.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parent proposal identifier.</summary>
    public Guid ProposalId { get; set; }


    /// <summary>Gets or sets the user who authored the comment.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the comment body text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the current active version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the record.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the record.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
