using System.Collections.Generic;
using System;
using Fdw.Configuration;

namespace Fdw.Services.Connections;

/// <summary>
/// The contract every DataStore implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be registered
/// against this domain or handed back by a read of it. The properties here are the ones every
/// DataStore implementation has -- an MsSql store and an HTTP store both hang off a connection and
/// both track discovery -- which is why they sit on the contract rather than being repeated.
/// </remarks>
public interface IDataStoreImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the connection this store is reached through.</summary>
    Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets how writes are applied to this store.</summary>
    string? WriteMode { get; set; }

    /// <summary>Gets or sets when this store's schema was last discovered.</summary>
    DateTimeOffset? LastDiscoveredAt { get; set; }

    /// <summary>Gets or sets whether this store is in use.</summary>
    bool IsActive { get; set; }

    /// <summary>Gets or sets the paths (schemas) within this store, cascaded on read.</summary>
    List<DataPathConfiguration> Paths { get; set; }
}
