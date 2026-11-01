using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A data view as a list shows it: <see cref="DataViewResponse"/> without its filters.</summary>
/// <remarks>
/// A page of views is one read of the variation domain table, which holds everything here. What a view draws (its
/// encoding and chart type) and its filters live on its implementation and come with the read of one view (FDW-1008).
/// </remarks>
public class DataViewSummaryResponse : ResourceSummary
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the kind of view it is: the name of its implementation, <c>DataView</c> for one a user saved. Null only if
    /// the domain row names none, which the list's filter to data views makes impossible.
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets the name of the view this one is a view of; null when it is a view of a data set.</summary>
    public string? ParentName { get; set; }

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data set this view reads.</summary>
    public Guid SubjectDataSetId { get; set; }

    /// <summary>Gets or sets the id of the user who owns the view.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Gets or sets when the view was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
