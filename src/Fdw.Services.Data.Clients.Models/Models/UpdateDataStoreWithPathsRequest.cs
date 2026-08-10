using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request to update a DataStore with paths.
/// </summary>
public sealed class UpdateDataStoreWithPathsRequest
{
    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the associated connection name.</summary>
    // Why: a DataStore's transport IS its connection's transport — the server derives StoreType from
    // the resolved connection, never from a client-supplied value, so StoreType is not on the wire contract.
    public string ConnectionName { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the data store is active.</summary>
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the write mode.</summary>
    public string? WriteMode { get; set; }
    /// <summary>Gets or sets the list of paths to maintain.</summary>
    public IReadOnlyList<DataPathRequest> Paths { get; set; } = Array.Empty<DataPathRequest>();
}
