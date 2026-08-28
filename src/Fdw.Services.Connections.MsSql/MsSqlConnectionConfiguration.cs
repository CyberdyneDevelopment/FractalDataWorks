using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.Connections.MsSql.Limits;

namespace Fdw.Services.Connections.MsSql;

/// <summary>
/// Configuration for Microsoft SQL Server connections.
/// Standalone typed body POCO — no longer inherits from <see cref="Fdw.Services.Connections.ConnectionConfiguration"/>.
/// Persisted to <c>conn.MsSqlConnection</c> as a child of <c>conn.Connection</c> via <see cref="ConnectionId"/>.
/// </summary>
/// <remarks>
/// <para>
/// Configuration patterns: <b>Pattern A</b> (typed columns: Server, Database, Port, etc.) +
/// <b>Pattern B</b> (typed-body specialization: conn.MsSqlConnection.ConnectionId FK to conn.Connection.Id) +
/// <b>Pattern C</b> (PropertyCollection: <c>AdditionalProperties</c> dict bound via DataContainerKey seed row
/// <c>TypeId='PropertyCollection', Name='Authentication'</c> → child container conn.MsSqlAuthentication).
/// </para>
/// <para>
/// The endpoint creates a <see cref="Fdw.Services.Connections.ConnectionConfiguration"/>
/// first (writing conn.Connection), then creates this record with <see cref="ConnectionId"/> pointing to
/// the parent's <see cref="Fdw.Configuration.IGenericConfiguration.Id"/>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "MsSql")]
public partial class MsSqlConnectionConfiguration : IConnectionImplementationConfiguration
{
    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (conn.MsSqlConnection.Id).
    /// Minted by <see cref="Fdw.Services.Configuration.ImplementationConfigurationProviderBase{TConfig,TCommand}"/>
    /// via <see cref="Guid.CreateVersion7()"/> when <see cref="Guid.Empty"/>.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the FK to <c>conn.Connection.Id</c> (the parent header row).
    /// Set by the endpoint before calling Save on this provider.
    /// </summary>
    public Guid ConnectionId { get; set; }


    string IGenericConfiguration.Name
    {
        get => string.Empty;
        set { /* typed body has no independent name — it is identified by ConnectionId */ }
    }

    string IGenericConfiguration.SectionName => "Connections";
    string IGenericConfiguration.ServiceType => "Connection";
    string? IGenericConfiguration.ServiceOptionType => "MsSql";

    #region SQL Server Specific Properties

    /// <summary>
    /// Gets or sets the SQL Server hostname or IP address.
    /// </summary>
    public string Server { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SQL Server port. Default is 1433.
    /// </summary>
    public int Port { get; set; } = 1433;

    /// <summary>
    /// Gets or sets the SQL Server instance name (for named instances).
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Gets or sets the authentication type discriminator.
    /// </summary>
    /// <remarks>
    /// Why: empty string, never null, is the "not yet supplied" marker — never a chosen business
    /// default (no "WindowsAuth", no guessed auth method). MsSqlConnectionFactory already fails
    /// loud via <c>string.IsNullOrEmpty(authTypeName)</c> when this is empty.
    /// </remarks>
    [ValuesFrom(typeof(MsSqlAuthenticationTypes))]
    public string AuthenticationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication key-value configuration.
    /// </summary>
    /// <remarks>
    /// Populated by the gateway cascade from conn.MsSqlAuthentication (Wave 0.6 PropertyCollection).
    /// The binding is declared in a <c>data.DataContainerKey</c> seed row:
    /// <c>TypeId='PropertyCollection', Name='Authentication'</c>.
    /// [NotMapped] because the values live in the child KVP table, not a column on this row.
    /// The factory resolves the typed authentication instance via MsSqlAuthenticationTypes.ByName(AuthenticationType).
    /// </remarks>
    [NotMapped]
    [ConfigurationChildTable("MsSqlConnectionProperty")]
    public IDictionary<string, string?> AdditionalProperties { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the default schema to use for operations.
    /// </summary>
    public string DefaultSchema { get; set; } = "dbo";

    /// <summary>
    /// Gets or sets a value indicating whether to trust the server certificate.
    /// </summary>
    public bool TrustServerCertificate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to encrypt the connection.
    /// </summary>
    public bool Encrypt { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable connection pooling.
    /// </summary>
    public bool EnableConnectionPooling { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum pool size.
    /// </summary>
    public int MinPoolSize { get; set; }

    /// <summary>
    /// Gets or sets the maximum pool size.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to enable multiple active result sets (MARS).
    /// </summary>
    public bool EnableMultipleActiveResultSets { get; set; }

    /// <summary>
    /// Gets or sets the application name to use in the connection.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically discover and persist schema on connection.
    /// </summary>
    public bool AutoDiscoverSchema { get; set; }

    /// <summary>
    /// Gets or sets the ID of the DataStore created from schema discovery on this connection.
    /// </summary>
    public Guid? AssociatedDataStoreId { get; set; }

    /// <summary>
    /// Gets or sets when schema was last imported for this connection.
    /// </summary>
    public DateTimeOffset? LastSchemaImportDate { get; set; }

    /// <summary>
    /// Gets or sets the active connection limits for this connection.
    /// Multiple limit kinds (RateLimit, QueryTimeout, etc.) can be active simultaneously.
    /// </summary>
    /// <remarks>
    /// [NotMapped] — loaded from conn.MsSqlConnectionLimit header+subtype tables by the cascade loader.
    /// </remarks>
    [NotMapped]
    public IList<MsSqlConnectionLimitConfiguration> Limits { get; set; } = new List<MsSqlConnectionLimitConfiguration>();

    #endregion
}
