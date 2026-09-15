using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Creates a saved view.</summary>
public class CreateSavedViewRequest
{
    /// <summary>Gets or sets the unique view name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data set this view reads.</summary>
    public Guid SubjectDataSetId { get; set; }

    /// <summary>Gets or sets the visualiser's own encoding, round-tripped opaquely.</summary>
    public string Encoding { get; set; } = string.Empty;

    /// <summary>Gets or sets the stored filter state, round-tripped opaquely.</summary>
    public string? Filters { get; set; }

    /// <summary>Gets or sets the chart kind.</summary>
    public string ChartType { get; set; } = string.Empty;
}
