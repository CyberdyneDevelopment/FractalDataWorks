using System;
using System.Collections.Generic;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Response from a schema sync operation via the API client.
/// </summary>
public sealed class SyncSchemaResponse
{
    /// <summary>Gets or sets the DataStore name.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether changes were detected.</summary>
    public bool HasChanges { get; set; }

    /// <summary>Gets or sets whether changes were applied.</summary>
    public bool ChangesApplied { get; set; }

    /// <summary>Gets or sets tables that were added.</summary>
    public IList<string> AddedTables { get; set; } = [];

    /// <summary>Gets or sets tables that were modified.</summary>
    public IList<string> ModifiedTables { get; set; } = [];

    /// <summary>Gets or sets tables that were removed.</summary>
    public IList<string> RemovedTables { get; set; } = [];

    /// <summary>Gets or sets the sync timestamp.</summary>
    public DateTime SyncedAt { get; set; }
}
