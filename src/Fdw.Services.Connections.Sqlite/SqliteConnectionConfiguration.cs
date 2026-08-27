using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Sqlite.Authentication;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Configuration for SQLite connections.
/// Persisted to <c>conn.SqliteConnection</c> as a child of <c>conn.Connection</c> via <see cref="ConnectionId"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "Sqlite")]
public partial class SqliteConnectionConfiguration : IConnectionImplementationConfiguration
{
    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (conn.SqliteConnection.Id).
    /// Minted by the provider via <see cref="Guid.CreateVersion7()"/> when <see cref="Guid.Empty"/>.
    /// </summary>
    // Why: NO Guid default — the provider mints this before INSERT. A random default here would
    // bypass the provider's Id-mint logic silently.
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the FK to <c>conn.Connection.Id</c> (the parent header row).
    /// </summary>
    public Guid ConnectionId { get; set; }

    // Why: Name and SectionName are not meaningful on the typed body — the canonical name lives on
    // the parent ConnectionConfiguration. Empty string is the correct implementation.
    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — identified by ConnectionId */ }
    }

    string IGenericConfiguration.SectionName => "Connections";
    string IGenericConfiguration.ServiceType => "Connection";
    string? IGenericConfiguration.ServiceOptionType => "Sqlite";

    // ========================================
    // SQLite-specific properties
    // ========================================

    /// <summary>
    /// Gets or sets the SQLite data source (file path or :memory:).
    /// </summary>
    public string DataSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SQLite open mode.
    /// Valid values: ReadWriteCreate, ReadWrite, ReadOnly, Memory.
    /// </summary>
    public string Mode { get; set; } = "ReadWriteCreate";

    /// <summary>
    /// Gets or sets the SQLite cache mode.
    /// Valid values: Default, Shared, Private.
    /// </summary>
    public string Cache { get; set; } = "Default";

    /// <summary>
    /// Gets or sets a value indicating whether to enforce SQLite foreign key constraints.
    /// </summary>
    public bool ForeignKeys { get; set; }

    /// <summary>
    /// Gets or sets the default command timeout in seconds.
    /// </summary>
    public int DefaultTimeout { get; set; } = 30;

    // ========================================
    // Authentication (like every other connection: a selector + a KVP the auth method parses)
    // ========================================

    /// <summary>
    /// Gets or sets the authentication method name (a <see cref="SqliteAuthenticationTypes"/> option:
    /// "None" or "EncryptionKey"). The factory resolves the typed method via
    /// <c>SqliteAuthenticationTypes.ByName(AuthenticationType)</c> and delegates secret resolution to it.
    /// </summary>
    [ValuesFrom(typeof(SqliteAuthenticationTypes))]
    public string? AuthenticationType { get; set; } = "None";

    /// <summary>
    /// Gets or sets the authentication KVP for the selected method (e.g. SecretManagerName/SecretKeyName
    /// for EncryptionKey). Each <see cref="SqliteAuthenticationTypes"/> option declares and parses its
    /// own keys.
    /// </summary>
    /// <remarks>
    /// [NotMapped] because the values live in the child KVP table <c>conn.SqliteConnectionAuthentication</c>,
    /// not columns on this row — mirroring MsSql's <c>conn.MsSqlConnectionAuthentication</c>. SQLite is a
    /// connection like any other; it is not a special-cased pair of columns.
    /// </remarks>
    [NotMapped]
    [ConfigurationChildTable("SqliteConnectionProperty")]
    public IDictionary<string, string?> AdditionalProperties { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
