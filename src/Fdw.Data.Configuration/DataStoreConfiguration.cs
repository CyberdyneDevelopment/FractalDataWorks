using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fdw.Services.Connections;

/// <summary>
/// Base configuration class for all data store types.
/// Generates the parent table <c>data.DataStore</c> which contains core fields shared by all store types.
/// </summary>
/// <remarks>
/// <para>
/// A DataStore represents a physical storage location accessible via a Connection.
/// The same physical database may have multiple DataStores when accessed via different
/// Connections with different credentials/identities.
/// </para>
/// <para>
/// Hierarchy:
/// <list type="bullet">
/// <item><description><c>DataStore</c> (this) - Root configuration, references Connection</description></item>
/// <item><description><c>DataPath</c> - Navigation within store</description></item>
/// <item><description><c>DataContainer</c> - Physical schema at a path</description></item>
/// <item><description><c>DataContainerField</c> - Column/property definition</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataStore")]
public partial class DataStoreConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public DataStoreConfiguration() : this("DataStore", null, "DataStores")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "DataStore".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "MsSql", "Rest", "OData").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected DataStoreConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        TypeId = serviceOptionType;
        SectionName = sectionName;
    }


    /// <summary>
    /// Gets or sets the unique identifier for this data store.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name of this data store for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "DataStore" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the DataStore type discriminator (e.g., "MsSql", "Rest", "OData").
    /// Maps to the <c>TypeId</c> column on <c>data.DataStore</c>.
    /// </summary>
    [ValuesFrom(typeof(DataStoreTypes))]
    public string? TypeId { get; set; }

    /// <summary>
    /// Gets or sets the service option type.
    /// </summary>
    /// <remarks>
    /// Why: IGenericConfiguration.ServiceOptionType is the interface contract. DataStore rows carry
    /// the discriminator in the TypeId column (Wave A DDL rename). This property bridges the two —
    /// getting/setting TypeId so existing consumers that reference ServiceOptionType continue to work.
    /// </remarks>
    public string? ServiceOptionType
    {
        get => TypeId;
        set => TypeId = value;
    }

    /// <summary>
    /// Gets or sets the connection ID this data store is accessed through.
    /// Required - a DataStore cannot exist without a Connection.
    /// </summary>
    public Guid ConnectionId { get; set; }

    /// <summary>
    /// Gets the store type name. Alias for <see cref="TypeId"/>.
    /// </summary>
    public string? StoreType => TypeId;

    /// <summary>
    /// Gets or sets the optional description of this data store.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the write mode for data operations (e.g., Append, Upsert, Replace, Merge).
    /// </summary>
    public string? WriteMode { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last successful schema discovery operation.
    /// </summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }

    /// <summary>
    /// Gets the timestamp when the record was created in this system.
    /// </summary>
    public DateTimeOffset CreateDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the database user who created the record.
    /// </summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets the application user on whose behalf the record was created.
    /// </summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the record was last modified.
    /// </summary>
    public DateTimeOffset ModifyDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the database user who last modified the record.
    /// </summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application user on whose behalf the record was last modified.
    /// </summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the paths (schemas) within this data store.
    /// </summary>
    /// <remarks>
    /// List{T} is required for IOptions binding - configuration system needs concrete collection types.
    /// </remarks>
#pragma warning disable MA0016 // Prefer collection abstraction - required for IOptions binding
    public List<DataPathConfiguration> Paths { get; set; } = [];
#pragma warning restore MA0016

    /// <summary>
    /// Gets or sets the typed data store body for this header row.
    /// Populated on the read path after loading the typed body table row.
    /// Not persisted — the typed body is saved separately to its own table.
    /// </summary>
    /// <summary>
    /// Gets or sets the human-facing display name. Transient — not persisted to data.DataStore (no column yet).
    /// Populated from the create/update request and echoed back in the response for round-trip fidelity.
    /// </summary>
    [NotMapped]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether this data store is active. Transient — not persisted to data.DataStore.
    /// Echoed back from the create/update request for round-trip fidelity.
    /// </summary>
    [NotMapped]
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the typed configuration body. Not stored as a column — written separately and populated by the typed provider on read.</summary>
    [NotMapped]
    public IDataStoreConfiguration? Configuration { get; set; }
}
