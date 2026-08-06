using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Limits;
using Fdw.Services.Connections.Http.Security;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// Base configuration class for HTTP-based connections (REST, SOAP, GraphQL, etc.).
/// Standalone typed body POCO — no longer inherits from <see cref="Fdw.Services.Connections.ConnectionConfiguration"/>.
/// Provides HTTP-specific configuration options shared across all HTTP connection types.
/// </summary>
/// <remarks>
/// <para>
/// Configuration patterns: <b>Pattern A</b> (typed columns: BaseUrl, Protocol, TimeoutSeconds, etc.) +
/// <b>Pattern B</b> (typed-body specialization: conn.HttpConnection.ConnectionId FK to conn.Connection.Id) +
/// <b>Pattern C</b> (PropertyCollection: <c>AdditionalProperties</c> dict bound via DataContainerKey seed row
/// <c>TypeId='PropertyCollection', Name='Authentication'</c> → child container conn.HttpConnectionAuthentication).
/// </para>
/// <para>
/// Concrete implementations should use [ManagedConfiguration] with <c>ServiceCategory = "Connection"</c>
/// and <c>ServiceType = "Http"</c>.
/// </para>
/// </remarks>
public abstract class HttpConnectionConfigurationBase : IConnectionConfiguration
{
    // ========================================
    // IGenericConfiguration — typed body identity
    // ========================================

    /// <summary>
    /// Gets or sets the unique identifier for this typed body row (conn.HttpConnection.Id).
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
    string? IGenericConfiguration.ServiceOptionType => "Http";

    /// <summary>Gets the connection type name for this HTTP variant.</summary>
    public abstract string ConnectionType { get; }

    #region HTTP-Specific Properties

    /// <summary>
    /// Gets or sets the service lifetime for the connection.
    /// Valid values: "Singleton", "Scoped", "Transient".
    /// </summary>
    public string Lifetime { get; set; } = "Scoped";

    // Why: SecretManagerName/SecretKeyName are NOT properties on the connection — they are keys in the
    // authentication KVP (conn.HttpConnectionAuthentication), parsed by the selected
    // HttpAuthenticationTypes option. A connection is not a special-cased secret pair.

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
    /// </remarks>
    [ValuesFrom(typeof(HttpProtocols))]
    public string Protocol { get; set; } = "Rest";

    /// <summary>
    /// Gets or sets the timeout for HTTP requests in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

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
    [ValuesFrom(typeof(HttpAuthenticationTypes))]
    public string AuthenticationType { get; set; } = "None";

    /// <summary>
    /// Gets or sets the authentication key-value configuration for the selected method (SecretManagerName
    /// plus the method's own secret-name keys). Each <see cref="HttpAuthenticationTypes"/> option parses
    /// its own keys.
    /// </summary>
    /// <remarks>
    /// Populated by the gateway cascade from <c>conn.HttpConnectionAuthentication</c>.
    /// [NotMapped] because the values live in the child KVP table, not columns on this row.
    /// </remarks>
    [NotMapped]
    [Fdw.Data.ConfigurationChildTable("HttpConnectionAuthentication")]
    public IDictionary<string, string?> AdditionalProperties { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

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
