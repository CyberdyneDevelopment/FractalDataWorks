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

    /// <summary>Gets or sets the write mode for ETL target operations.</summary>
    public string? WriteMode { get; set; }

    /// <summary>Gets or sets the human-facing display name. Falls back to Name when null.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets whether this data store is active.</summary>
    public bool IsActive { get; set; }
}
