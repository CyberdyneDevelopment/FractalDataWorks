namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Request for syncing schema changes via the API client.
/// </summary>
public sealed class SyncSchemaRequest
{
    /// <summary>
    /// Gets or sets the connection name.
    /// </summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to apply detected changes.
    /// </summary>
    public bool ApplyChanges { get; set; }
}
