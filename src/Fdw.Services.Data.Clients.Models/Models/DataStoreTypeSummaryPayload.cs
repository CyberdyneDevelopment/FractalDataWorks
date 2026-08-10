namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Summary payload for an available DataStore type, sourced from the DataStoreTypes TypeCollection.
/// </summary>
public sealed class DataStoreTypeSummaryPayload
{
    /// <summary>Gets or sets the type name (e.g., "MsSql", "Http", "FileSystem").</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-facing display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of this DataStore type.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the category (always "DataStore" for DataStore types).</summary>
    public string Category { get; set; } = string.Empty;
}
