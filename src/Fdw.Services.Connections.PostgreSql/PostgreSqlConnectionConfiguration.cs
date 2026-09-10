using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.PostgreSql.Authentication;

namespace Fdw.Services.Connections.PostgreSql;

/// <summary>
/// Configuration for PostgreSQL connections.
/// Standalone typed body POCO — no longer inherits from <c>IConnectionImplementationConfiguration</c>.
/// Persisted to <c>conn.PostgreSqlConnection</c> as a child of <c>conn.Connection</c> via <see cref="ConnectionId"/>.
/// </summary>
/// <remarks>
/// <para>
/// Configuration patterns: <b>Pattern A</b> (typed columns: Host, Database, Port, SslMode, etc.) +
/// <b>Pattern B</b> (typed-body specialization: conn.PostgreSqlConnection.ConnectionId FK to conn.Connection.Id) +
/// <b>Pattern C</b> (PropertyCollection: <c>AdditionalProperties</c> dict bound via DataContainerKey seed row
/// <c>TypeId='PropertyCollection', Name='Authentication'</c> → child container conn.PostgreSqlConnectionAuthentication).
/// </para>
/// <para>
/// The endpoint creates a <c>IConnectionImplementationConfiguration</c>
/// first (writing conn.Connection), then creates this record with <see cref="ConnectionId"/> pointing to
/// the parent's <see cref="Fdw.Configuration.IGenericConfiguration.Id"/>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Connection", ServiceType = "PostgreSql")]
public partial class PostgreSqlConnectionConfiguration : IConnectionImplementationConfiguration
{
    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public string? Environment { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckEnabled { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckOnStartup { get; set; }

    /// <inheritdoc/>
    public int? HealthCheckIntervalSeconds { get; set; }

    /// <inheritdoc/>
    public bool DiscoveryEnabled { get; set; } = true;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (conn.PostgreSqlConnection.Id).
    /// Minted by the implementation provider
    /// via <see cref="Guid.CreateVersion7()"/> when <see cref="Guid.Empty"/>.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the FK to <c>conn.Connection.Id</c> (the parent header row).
    /// Set by the endpoint before calling Save on this provider.
    /// </summary>
    public Guid ConnectionId { get; set; }


    #region PostgreSQL Specific Properties

    /// <summary>
    /// Gets or sets the PostgreSQL server hostname or IP address.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PostgreSQL port. Default is 5432.
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Gets or sets the SSL mode for the connection.
    /// </summary>
    /// <remarks>
    /// Supported values: Disable, Allow, Prefer, Require, VerifyCA, VerifyFull.
    /// Default is "Prefer".
    /// </remarks>
    public string SslMode { get; set; } = "Prefer";

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 15;

    /// <summary>
    /// Gets or sets the default schema to use for operations.
    /// </summary>
    public string DefaultSchema { get; set; } = "public";

    /// <summary>
    /// Gets or sets the maximum pool size.
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the minimum pool size.
    /// </summary>
    public int MinPoolSize { get; set; }

    /// <summary>
    /// Gets or sets the application name to report to PostgreSQL.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the authentication method discriminator (a <see cref="PostgreSqlAuthenticationTypes"/>
    /// option: None/Password). The factory resolves the typed method via
    /// <c>PostgreSqlAuthenticationTypes.ByName(AuthenticationType)</c> and delegates secret resolution to it.
    /// </summary>
    /// <remarks>
    /// Why: empty string, never null, is the "not yet supplied" marker — "None" is a real,
    /// selectable auth option, not a silently-assumed default. PostgreSqlConnectionFactory already
    /// fails loud via <c>string.IsNullOrEmpty(authTypeName)</c> when this is empty.
    /// </remarks>
    [ValuesFrom(typeof(PostgreSqlAuthenticationTypes))]
    public string AuthenticationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication key-value configuration for the selected method (SecretManagerName
    /// plus the method's own secret-name keys). Each <see cref="PostgreSqlAuthenticationTypes"/> option
    /// parses its own keys.
    /// </summary>
    /// <remarks>
    /// Populated by the gateway cascade from conn.PostgreSqlConnectionAuthentication.
    /// [NotMapped] because the values live in the child KVP table, not a column on this row.
    /// </remarks>
    [NotMapped]
    [ConfigurationChildTable("PostgreSqlConnectionProperty")]
    public IDictionary<string, string?> AdditionalProperties { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    #endregion
}
