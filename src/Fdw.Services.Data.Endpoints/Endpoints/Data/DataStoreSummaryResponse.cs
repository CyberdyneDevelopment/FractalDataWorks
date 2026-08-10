using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Summary DTO for a data store, used in list responses.
/// </summary>
public class DataStoreSummaryResponse : ResourceSummary
{
    /// <summary>Gets or sets the data store type (e.g., MsSql).</summary>
    public string? StoreType { get; set; }

    /// <summary>Gets or sets the connection identifier.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the connection name for display and schema discovery.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the number of paths (schemas) in the data store.</summary>
    public int PathCount { get; set; }

    /// <summary>Gets or sets the total number of containers across all paths.</summary>
    public int ContainerCount { get; set; }

    /// <summary>Gets or sets the last time the DataStore schema was refreshed.</summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }

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
