using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Summary DTO for a data set, used in list responses.
/// </summary>
public class DataSetSummaryResponse : ResourceSummary
{
    /// <summary>Gets or sets the data set identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the short abbreviation for compact UI contexts.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the data set description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data set category.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the data set version.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the number of fields in the data set.</summary>
    public int FieldCount { get; set; }

    /// <summary>Gets or sets the number of sources in the data set.</summary>
    public int SourceCount { get; set; }

    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the last modification timestamp.</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Gets or sets the user who created the record.</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the user who last modified the record.</summary>
    public string ModifiedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was created.</summary>
    public string CreatedOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the record was last modified.</summary>
    public string ModifiedOnBehalfOf { get; set; } = string.Empty;
}
