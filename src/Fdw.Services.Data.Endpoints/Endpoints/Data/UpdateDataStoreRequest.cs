using System.Collections.Generic;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for updating an existing data store.
/// </summary>
/// <remarks>
/// Why: Same as CreateDataStoreRequest — the UI sends ConnectionName (string), not ConnectionId (Guid).
/// The endpoint base class resolves ConnectionName → ConnectionId via IOptionsMonitor before persisting.
/// </remarks>
public class UpdateDataStoreRequest
{
    /// <summary>Gets or sets the data store name (identifier).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated store type (e.g., "MsSql"). Maps to ServiceOptionType on the configuration.</summary>
    public string? StoreType { get; set; }

    /// <summary>Gets or sets the updated connection name. Resolved to ConnectionId by the endpoint.</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets the updated description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the updated write mode for ETL target operations.</summary>
    public string? WriteMode { get; set; }

    /// <summary>Gets or sets the updated human-facing display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the updated active state. Null means keep existing.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Gets or sets the paths this store exposes, replacing the ones it has.</summary>
    /// <remarks>
    /// Why the whole collection rather than a delta: the provider's Save cascades an aggregate's
    /// children, so what is sent is what the store ends up with. A caller sending a shorter list
    /// means those paths are gone, which is the same shape the dataset update already uses for its
    /// fields and sources.
    ///
    /// Null is distinct from empty here: null leaves the existing paths alone, empty removes them.
    /// The client has sent these since it was written; the request it binds to had nowhere to put
    /// them, so they were dropped before reaching the provider.
    /// </remarks>
    public IList<DataPathRequest>? Paths { get; set; }
}
