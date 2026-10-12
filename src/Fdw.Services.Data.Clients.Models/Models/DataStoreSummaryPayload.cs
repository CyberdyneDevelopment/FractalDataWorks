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
    /// <summary>Gets or sets the last time the DataStore schema was refreshed.</summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }
    /// <summary>Gets or sets the creation timestamp. The other audit stamps are on the data store's own read, not on a list row.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
