using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data view, whole: what a create and a read of one view answer.</summary>
/// <remarks>The list answers <see cref="DataViewSummaryResponse"/>, which leaves out <c>Filters</c>: a list is
/// one read of the domain table, and the filters live on the implementation (FDW-1008).</remarks>
public class DataViewResponse : ResourceSummary
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the kind of view it is: the name of its implementation, <c>DataView</c> for one a user saved.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the view this one is a view of; null when it is a view of a data set.</summary>
    public string? ParentName { get; set; }

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

    /// <summary>Gets or sets the owning user.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Gets or sets when the view was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
