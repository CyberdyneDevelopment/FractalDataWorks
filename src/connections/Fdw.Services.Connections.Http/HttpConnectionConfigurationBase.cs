using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Conventions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Limits;
using Fdw.Services.Connections.Http.Paging;
using Fdw.Services.Connections.Http.Security;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// Base configuration class for HTTP-based connections (REST, SOAP, GraphQL, etc.).
/// The HTTP implementation of the Connection domain (<see cref="IConnectionImplementationConfiguration"/>).
/// Provides HTTP-specific configuration options shared across all HTTP connection types.
/// </summary>
/// <remarks>
/// <para>
/// Configuration patterns: <b>Pattern A</b> (typed columns: BaseUrl, Protocol, TimeoutSeconds, etc.) +
/// <b>Pattern B</b> (the implementation row: conn.HttpConnection.ConnectionId FK to conn.Connection.Id) +
/// <b>Pattern C</b> (PropertyCollection: <c>AdditionalProperties</c> dict bound via DataContainerKey seed row
/// <c>TypeId='PropertyCollection', Name='Authentication'</c> → child container conn.HttpConnectionAuthentication).
/// </para>
/// <para>
/// Concrete implementations should use [ManagedConfiguration] with <c>ServiceCategory = "Connection"</c>
/// and <c>ServiceType = "Http"</c>.
/// </para>
/// </remarks>
public abstract class HttpConnectionConfigurationBase : IConnectionImplementationConfiguration
{
    /// <inheritdoc/>
    public string? Description { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset CreateDate { get; set; }

    /// <inheritdoc/>
    public DateTimeOffset ModifyDate { get; set; }

    /// <inheritdoc/>
    public string? Environment { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckEnabled { get; set; }

    /// <inheritdoc/>
    public bool HealthCheckOnStartup { get; set; }

    /// <inheritdoc/>
    public int? HealthCheckIntervalSeconds { get; set; }

    /// <inheritdoc/>
    [ValuesFrom(typeof(SchemaDiscoveryTypes))]
    public string SchemaDiscovery { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    // ========================================
    // IGenericConfiguration — identity
    // ========================================

    /// <summary>
    /// Gets or sets the identifier (conn.HttpConnection.Id). The Connection domain stamps its domain row's Id here
    /// when it saves the pair.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;


    /// <summary>Gets the connection type name for this HTTP variant.</summary>
    public abstract string ConnectionType { get; }

    #region HTTP-Specific Properties

    /// <summary>
    /// Gets or sets the service lifetime for the connection.
    /// Valid values: "Singleton", "Scoped", "Transient".
    /// </summary>
    /// <remarks>
    /// Not stored and not read (FDW-5139): conn.HttpConnectionConfiguration has no column for it, so a read leaves it empty, and the
    /// validator does not require it. Removing it, with HttpConnectionType's declared default for it, is the follow-up.
    /// </remarks>
    public string Lifetime { get; set; } = string.Empty;


    /// <summary>
    /// Gets or sets the base URL for the HTTP connection.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP protocol type name.
    /// </summary>
    /// <remarks>
    /// Supported values:
    /// <list type="bullet">
    /// <item><description>"Rest" - RESTful API protocol</description></item>
    /// <item><description>"Soap11" - SOAP 1.1 protocol</description></item>
    /// <item><description>"Soap12" - SOAP 1.2 protocol</description></item>
    /// <item><description>"GraphQL" - GraphQL protocol</description></item>
    /// <item><description>"OData" - OData protocol</description></item>
    /// <item><description>Custom protocol names registered via TypeOption</description></item>
    /// </list>
    /// FDW chooses no value for it: the connection states it, and the validator refuses an empty one (FDW-3580).
    /// </remarks>
    [ValuesFrom(typeof(HttpProtocols))]
    public string Protocol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how the remote pages its rows: the name of an <see cref="HttpPagingStyles"/> option (SingleResponse, OffsetLimit,
    /// PageNumber, LinkHeader, BodyNextLink, ContinuationToken), or <see langword="null"/> when the connection states none.
    /// </summary>
    /// <remarks>
    /// FDW chooses no style (FDW-4938): it is a choice only the caller makes, so a create (and a test of an unsaved configuration) that leaves
    /// it out is refused, naming it (FDW-5014). A stored row from before the column states none; a paged or streamed read of it is refused,
    /// naming it, and every other use of it works as before. The style's own settings (page size, parameter names, the path to a next link or
    /// token) are entries of <see cref="AdditionalProperties"/> named <c>Paging...</c> (<see cref="HttpPagingProperties"/>), and the style
    /// refuses a connection that leaves out one it needs.
    /// </remarks>
    [CallerChoice]
    [ValuesFrom(typeof(HttpPagingStyles))]
    public string? PagingStyle { get; set; }

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds, or <see langword="null"/> when the connection states none.
    /// </summary>
    /// <remarks>
    /// No number is assumed here or in the column. A connection that states none takes the default its connection type declares
    /// (<c>HttpConnectionType</c>'s DeclaredDefaults), which a host changes with <c>ServiceTypeDefaults.Override</c> before Register (FDW-4904). A create that leaves it out stores
    /// nothing (<see cref="ResolvedAtUseAttribute"/>), so the default is resolved each time the connection is built.
    /// </remarks>
    [ResolvedAtUse]
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the default content type for requests.
    /// If not specified, the protocol's default content type is used.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets whether the resolved WS-Security certificate is attached to
    /// HttpClientHandler.ClientCertificates for transport-level mutual TLS.
    /// </summary>
    public bool UseMtls { get; set; }

    /// <summary>
    /// Gets or sets the authentication method discriminator (a <see cref="HttpAuthenticationTypes"/>
    /// option: None/Basic/Bearer/ApiKey/UsernameToken/WsSecurity). The factory resolves the typed method
    /// via <c>HttpAuthenticationTypes.ByName(AuthenticationType)</c> and delegates secret resolution to it.
    /// </summary>
    /// <remarks>
    /// FDW chooses no value for it: the connection states it, and the validator refuses an empty one (FDW-3580).
    /// </remarks>
    [ValuesFrom(typeof(HttpAuthenticationTypes))]
    public string AuthenticationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication key-value configuration for the selected method (SecretManagerName
    /// plus the method's own secret-name keys). Each <see cref="HttpAuthenticationTypes"/> option parses
    /// its own keys. The paging style the connection names keeps its plain <c>Paging...</c> settings here too (FDW-4938).
    /// </summary>
    /// <remarks>
    /// Populated by the gateway cascade from <c>conn.HttpConnectionAuthentication</c>.
    /// [NotMapped] because the values live in the child KVP table, not columns on this row.
    /// </remarks>
    [NotMapped]
    [Fdw.Data.ConfigurationChildTable("HttpConnectionProperty")]
    public IDictionary<string, string?> AdditionalProperties { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets request headers sent with every request this connection makes.
    /// </summary>
    /// <remarks>
    /// Populated by the gateway cascade from <c>conn.HttpConnectionHeader</c>, the same KVP mechanism
    /// <see cref="AdditionalProperties"/> uses. [NotMapped] because the values live in the child table,
    /// not columns on this row.
    /// <para>
    /// Why this exists: the factory sets Accept and a User-Agent when it builds the client, and those
    /// were the only headers a connection could ever send. Real APIs disagree about what they will
    /// accept — ESPN's site API answers 200 for one User-Agent and 403 for another — so the value has
    /// to belong to the connection, not to the framework. A header configured here overrides the
    /// factory's, which makes User-Agent settable without special-casing it.
    /// </para>
    /// </remarks>
    [NotMapped]
    [Fdw.Data.ConfigurationChildTable("HttpConnectionHeader")]
    public IDictionary<string, string?> Headers { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets SOAP-specific settings.
    /// </summary>
    public HttpSoapSettings? Soap { get; set; }

    /// <summary>
    /// Gets or sets the active connection limits for this Http connection.
    /// Multiple limit kinds can be active simultaneously.
    /// </summary>
    /// <remarks>
    /// [NotMapped] — loaded from conn.HttpConnectionLimit header+subtype tables by the cascade loader.
    /// </remarks>
    [NotMapped]
    public IList<HttpConnectionLimitConfiguration> Limits { get; set; } = new List<HttpConnectionLimitConfiguration>();

    #endregion
}
