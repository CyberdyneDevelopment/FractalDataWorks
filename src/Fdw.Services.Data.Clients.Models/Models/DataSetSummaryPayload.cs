using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Summary information for a DataSet.
/// </summary>
public sealed class DataSetSummaryPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the name (stable identifier).</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the short abbreviation for compact UI contexts.</summary>
    public string? Abbreviation { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the version.</summary>
    public string Version { get; set; } = "1.0";
    /// <summary>Gets or sets the name of the category the data set sits under; null when it sits under none.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the display names from the top of the category tree down to the data set's category, joined by " / "; null when it sits under none.</summary>
    public string? CategoryPath { get; set; }
    /// <summary>Gets or sets the service option type.</summary>
    public string Implementation { get; set; } = "Standard";
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
