using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Configuration class for data paths within a DataStore.
/// Generates the table <c>data.DataPath</c> as a child of <c>data.DataStore</c>.
/// </summary>
/// <remarks>
/// <para>
/// A DataPath represents navigation to a specific location within a store:
/// <list type="bullet">
/// <item><description>SQL: <c>dbo.Customers</c>, <c>sales.Orders</c></description></item>
/// <item><description>REST: <c>/api/v1/customers</c>, <c>/api/v1/orders/{id}</c></description></item>
/// <item><description>File: <c>/data/customers/*.csv</c></description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "DataStore",
    ServiceType = "DataPath")]
public partial class DataPathConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataPathConfiguration"/> class.
    /// </summary>
    public DataPathConfiguration()
    {
    }


    /// <summary>
    /// Gets or sets the unique identifier for this data path.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name of this path for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for configuration binding.
    /// </summary>
    public string SectionName => "DataPaths";

    /// <summary>
    /// Gets the service type - always "DataStore" for DataPath.
    /// </summary>
    public string ServiceType => "DataStore";

    /// <summary>
    /// Gets the service option type - null for base DataPath.
    /// </summary>
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the DataStore ID this path belongs to.
    /// </summary>
    public Guid DataStoreId { get; set; }

    /// <summary>
    /// Gets or sets the full path string (e.g., "dbo.Customers", "/api/v1/orders").
    /// </summary>
    /// <remarks>
    /// Named PathValue and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this value and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the DataPath type discriminator (e.g., "Schema", "Directory", "UrlPrefix").
    /// Maps to the <c>TypeId</c> column on <c>data.DataPath</c>.
    /// </summary>
    // Why: The SQL column is TypeId (Wave A DDL). [GenerateMapper] maps C# property names to SQL
    // column names; this property must be named TypeId to bind correctly.
    public string? TypeId { get; set; }

    /// <summary>
    /// Gets or sets the path type. Alias for <see cref="TypeId"/>.
    /// </summary>
    /// <remarks>
    /// Why: Legacy consumers reference PathType. Bridge property keeps them working while
    /// the column name in the database is TypeId.
    /// </remarks>
    public string? PathType
    {
        get => TypeId;
        set => TypeId = value;
    }

    /// <summary>
    /// Gets or sets the optional description for this path.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the source-discovered description (e.g., MS_Description from SQL Server).
    /// Used as fallback when <see cref="Description"/> is not set.
    /// </summary>
    public string? SourceDescription { get; set; }

    /// <summary>
    /// Gets or sets the containers (tables/endpoints) within this path.
    /// </summary>
    /// <remarks>
    /// List{T} is required for IOptions binding - configuration system needs concrete collection types.
    /// </remarks>
#pragma warning disable MA0016 // Prefer collection abstraction - required for IOptions binding
    public List<DataContainerConfiguration> Containers { get; set; } = [];

    /// <summary>
    /// Gets or sets the authorization policies attached to this DataPath.
    /// Populated at runtime by the DataStore config provider from <c>data.DataPathPolicy</c>.
    /// Not persisted in the primary DataPath row — set by cascade load.
    /// </summary>
    /// <remarks>
    /// Why: [NotMapped] marks this as a runtime-only child cascade so the source generator
    /// does not include Policies in the DDL for data.DataPath. The child rows live in
    /// data.DataPathPolicy and are joined on DataPathRowId (physical FK).
    /// </remarks>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<DataPathPolicyConfiguration> Policies { get; set; } = [];

    /// <summary>
    /// Gets or sets the per-extension file-type handler overrides for this DataPath.
    /// Populated at runtime by the DataStore config provider from <c>data.FileTypeHandlerOverride</c>.
    /// Not persisted in the primary DataPath row — set by cascade load.
    /// </summary>
    /// <remarks>
    /// Why: [NotMapped] marks this as a runtime-only child cascade so the source generator
    /// does not include FileTypeHandlerOverrides in the DDL for data.DataPath. The child rows
    /// live in data.FileTypeHandlerOverride and are joined on DataPathRowId (physical FK).
    /// </remarks>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public List<FileTypeHandlerOverrideConfiguration> FileTypeHandlerOverrides { get; set; } = [];
#pragma warning restore MA0016

}
