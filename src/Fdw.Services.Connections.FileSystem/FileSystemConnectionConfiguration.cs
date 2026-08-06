using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// Configuration for FileSystem connections.
/// Standalone typed body POCO — no longer inherits from <see cref="Fdw.Services.Connections.ConnectionConfiguration"/>.
/// Persisted to <c>conn.FileSystemConnection</c> as a child of <c>conn.Connection</c> via <see cref="ConnectionId"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "FileSystem")]
public partial class FileSystemConnectionConfiguration : IConnectionConfiguration
{
    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (conn.FileSystemConnection.Id).
    /// Minted by <see cref="Fdw.Services.Configuration.DefaultConfigurationProvider{TConfig,TCommand}"/>
    /// via <see cref="Guid.CreateVersion7()"/> when <see cref="Guid.Empty"/>.
    /// </summary>
    // Why: NO Guid.NewGuid() default — the provider mints this before INSERT via CreateVersion7().
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the FK to <c>conn.Connection.Id</c> (the parent header row).
    /// Set by the endpoint before calling Save on this provider.
    /// </summary>
    public Guid ConnectionId { get; set; }


    // Why: IGenericConfiguration members below satisfy the interface contract.
    // Name is not meaningful on the typed body — the canonical name lives on the parent
    // ConnectionConfiguration row. Typed-body providers never call Get(string name).
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — it is identified by ConnectionId */ }
    }

    string IGenericConfiguration.SectionName => "Connections";
    string IGenericConfiguration.ServiceType => "Connection";
    string? IGenericConfiguration.ServiceOptionType => "FileSystem";

    // ========================================
    // FileSystem-specific properties
    // ========================================

    /// <summary>
    /// Gets or sets the root directory path for this file system connection.
    /// </summary>
    public string Root { get; set; } = string.Empty;
}
