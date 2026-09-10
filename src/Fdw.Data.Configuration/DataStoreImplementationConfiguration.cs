using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Connections;

/// <summary>
/// The DataStore implementation's own configuration.
/// </summary>
/// <remarks>
/// Every property here was on the domain record until the split. None of it is something the
/// DataStore domain could have an opinion about for every implementation of itself, which is the
/// test for whether it belongs on the implementation.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "DataStore", ServiceType = "DataStore")]
public sealed partial class DataStoreImplementationConfiguration : IDataStoreImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain record's durable id.</summary>
    public Guid DataStoreId { get; set; }

    /// <summary>Gets or sets the domain record's row id -- the foreign key the constraint is on.</summary>
    public int DataStoreRowId { get; set; }

    /// <inheritdoc/>
    public Guid ConnectionId { get; set; }

    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? WriteMode { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset? LastDiscoveredAt { get; set; }

    /// <inheritdoc/>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the paths (schemas) within this store, cascaded on read.</summary>
    public List<DataPathConfiguration> Paths { get; set; } = [];
}
