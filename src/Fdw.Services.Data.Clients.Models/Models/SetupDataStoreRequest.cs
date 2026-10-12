using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request for the DataStore setup wizard endpoint.
/// Supports either an existing connection (by ID) or inline new connection config.
/// </summary>
public sealed class SetupDataStoreRequest
{
    /// <summary>Gets or sets the DataStore name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the connection type (e.g., "MsSql").</summary>
    public string ConnectionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of an existing connection to use for discovery.
    /// Takes precedence over <see cref="ExistingConnectionId"/> when both are provided.
    /// Mutually exclusive with <see cref="NewConnection"/>.
    /// </summary>
    public string? ExistingConnectionName { get; set; }

    /// <summary>
    /// Gets or sets the ID of an existing connection to use for discovery.
    /// Mutually exclusive with <see cref="NewConnection"/>.
    /// </summary>
    public Guid? ExistingConnectionId { get; set; }

    /// <summary>
    /// Gets or sets inline connection config to create and immediately use for discovery.
    /// Mutually exclusive with <see cref="ExistingConnectionId"/>.
    /// </summary>
    public SetupDataStoreNewConnectionRequest? NewConnection { get; set; }

    /// <summary>Gets or sets schemas to exclude from discovery (e.g., "sys", "information_schema").</summary>
    public IList<string>? ExcludeSchemas { get; set; }
}
