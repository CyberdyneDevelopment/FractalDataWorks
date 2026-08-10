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
}
