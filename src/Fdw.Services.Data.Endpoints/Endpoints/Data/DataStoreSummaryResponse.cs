using System;
using System.Text.Json.Serialization;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Summary DTO for a data store, used in list responses.
/// </summary>
public class DataStoreSummaryResponse : ResourceSummary
{
    /// <summary>Gets or sets the domain record's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the data store type (e.g., MsSql).</summary>
    public string? StoreType { get; set; }

    /// <summary>Gets or sets the connection identifier.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Gets or sets the connection name for display and schema discovery. Left out (null) when the caller may
    /// not see connections or the connection is not visible to them.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets the data store description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the data store's data classification level; <c>NotYetClassified</c> until someone picks a level.
    /// </summary>
    public int ClassificationId { get; set; }

    /// <summary>Gets or sets the last time the DataStore schema was refreshed.</summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp. The other audit stamps (who and when it was last modified, and on
    /// whose behalf) are not on a list row: the data store's own read carries them.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
