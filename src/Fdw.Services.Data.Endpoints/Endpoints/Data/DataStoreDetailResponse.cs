using System;
using System.Collections.Generic;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Detailed DTO for a data store, including paths and containers.
/// </summary>
public class DataStoreDetailResponse : ResourceDetail
{
    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets whether this data store is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the data store type (e.g., MsSql).</summary>
    public string? StoreType { get; set; }

    /// <summary>Gets or sets the connection identifier.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the name of the backing connection.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the write mode for ETL target operations.</summary>
    public string? WriteMode { get; set; }

    /// <summary>Gets or sets the paths (schemas) belonging to this data store.</summary>
    public IList<DataStorePathResponse> Paths { get; set; } = [];

    /// <summary>Gets or sets the last time the DataStore schema was discovered.</summary>
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
