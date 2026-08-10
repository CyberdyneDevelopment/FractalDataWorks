using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Summary information for a DataStore.
/// </summary>
public sealed class DataStoreSummaryPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the associated connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the count of associated paths.</summary>
    public int PathCount { get; set; }
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
