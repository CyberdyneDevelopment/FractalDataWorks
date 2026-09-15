using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>A saved view, in both its create and list shape.</summary>
/// <remarks>One shape for both: unlike a dataverse, a saved view has no children to omit from a
/// list projection, so there is nothing a detail view would show that a summary would not.</remarks>
public class SavedViewResponse : ResourceSummary
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

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
    public Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets when the view was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
