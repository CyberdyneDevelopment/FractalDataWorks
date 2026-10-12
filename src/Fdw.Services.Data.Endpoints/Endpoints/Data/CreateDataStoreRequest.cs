using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for creating a new data store.
/// </summary>
/// <remarks>
/// Why: The UI resolves connections by name (from dropdown), not by GUID. The endpoint
/// base class resolves ConnectionName → ConnectionId via IOptionsMonitor before persisting.
/// </remarks>
public class CreateDataStoreRequest
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection name. Resolved to ConnectionId by the endpoint.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the data store description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the data store type the new store has, one of the types the connection's kind can back (the
    /// connection type's <c>SupportedDataStoreTypes</c>). Left out, it is the kind's first.
    /// </summary>
    public string? StoreType { get; set; }

    /// <summary>
    /// Gets or sets the <c>ClassificationId</c> of the data store's data classification level (see
    /// <c>GET /data-classifications</c>). Left out, it is <c>NotYetClassified</c>. An id that is not a level is refused.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets the write mode for ETL target operations.</summary>
    public string? WriteMode { get; set; }

    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets whether this data store is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the paths the new store exposes, each with its containers, fields and keys.</summary>
    /// <remarks>
    /// The provider's Save cascades an aggregate's children, so the whole tree supplied here is written
    /// with the store in one call. Containers used to be dropped here silently - only a path's name,
    /// physical path and description were read - so each had to be added afterwards (FDW-920).
    /// </remarks>
    public IList<CreateDataStorePathRequest>? Paths { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the new store's schema is read from its connection and
    /// saved with it: every path and container the connection's schema discovery finds, with fields
    /// and keys, alongside whatever <see cref="Paths"/> supplies.
    /// </summary>
    public bool Discover { get; set; }
}
