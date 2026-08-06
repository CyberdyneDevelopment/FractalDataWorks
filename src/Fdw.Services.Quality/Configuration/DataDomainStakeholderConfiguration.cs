using System;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Maps to <c>data.DataDomainStakeholder</c> — a stakeholder assigned to a data domain.
/// Child of <see cref="DataDomainConfiguration"/> via DataDomainId FK.
/// </summary>
[GenerateMapper]
public sealed partial class DataDomainStakeholderConfiguration
{

    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the stakeholder record name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parent data domain identifier.</summary>
    public Guid DataDomainId { get; set; }


    /// <summary>Gets or sets the user identifier of the stakeholder.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the stakeholder role: "Owner", "Steward", "Consumer", or "Contributor".</summary>
    // Why: StakeholderRole is a TypeCollection-backed enum constrained at the DB level.
    public string StakeholderRole { get; set; } = string.Empty;

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
