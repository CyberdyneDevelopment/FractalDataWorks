using System;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Creates a data view.</summary>
public class CreateDataViewRequest
{
    /// <summary>Gets or sets the unique view name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data set this view reads; may be left empty when <see cref="ParentName"/> names the view it is a view of.</summary>
    public Guid SubjectDataSetId { get; set; }

    /// <summary>Gets or sets the name of the view this one is a view of, whose filters it adds to; null for a view of the data set itself.</summary>
    public string? ParentName { get; set; }

    /// <summary>Gets or sets the visualiser's own encoding, round-tripped opaquely; given with <see cref="ChartType"/> or not at all.</summary>
    public string? Encoding { get; set; }

    /// <summary>Gets or sets the stored filter state, round-tripped opaquely.</summary>
    public string? Filters { get; set; }

    /// <summary>Gets or sets the chart kind; given with <see cref="Encoding"/> or not at all, a view with neither draws no chart.</summary>
    public string? ChartType { get; set; }
}
