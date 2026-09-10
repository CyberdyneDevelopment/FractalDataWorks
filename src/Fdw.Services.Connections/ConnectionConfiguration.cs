using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Base configuration class for all connection types.
/// Generates the parent table <c>conn.Connection</c> which contains core identity fields shared by all connection types.
/// </summary>
/// <remarks>
/// <para>
/// This class serves two purposes:
/// <list type="bullet">
/// <item><description>As a header configuration for <c>IOptionsSnapshot&lt;List&lt;ConnectionConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>As the base class for type-specific configurations (MsSqlConnectionConfiguration, etc.)</description></item>
/// </list>
/// </para>
/// <para>
/// The implementation this record names is read from the row, never compiled into a type.
/// Derived classes call the protected constructor to set their specific values.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Connection")]
public partial class ConnectionConfiguration : IConnectionConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public ConnectionConfiguration() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="implementation">The implementation this record names (e.g., "MsSql", "Rest", "Http").</param>
    protected ConnectionConfiguration(string? implementation)
    {
        Implementation = implementation;
    }


    /// <summary>
    /// Gets or sets the durable logical identifier (matches conn.Connection.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this connection for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the domain this record belongs to.</summary>
    public string Domain => "Connection";

    /// <summary>Gets or sets the implementation this record names.</summary>
    [ValuesFrom(typeof(ConnectionTypes))]
    public string? Implementation { get; set; }

    /// <summary>
    /// Gets the connection type name. Alias for <see cref="Implementation"/>.
    /// </summary>
    public string? ConnectionType => Implementation;

    /// <summary>
    /// Gets or sets the optional description of this connection.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment this connection targets (e.g., Local, Dev, QA, Prod).
    /// </summary>
    [ValuesFrom(typeof(EnvironmentTypes))]
    public string? Environment { get; set; }


    /// <summary>
    /// Gets or sets whether the automated Connections domain health check
    /// (<see cref="Fdw.Services.Connections.ConnectionsHealthCheckable"/>) probes this connection.
    /// </summary>
    public bool HealthCheckEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether this connection should be probed once at host startup, in addition to
    /// any periodic check performed by the health monitor domain.
    /// </summary>
    public bool HealthCheckOnStartup { get; set; }

    /// <summary>
    /// Gets or sets the interval, in seconds, between periodic health checks for this connection.
    /// Null means no periodic interval is configured for this connection specifically — the health
    /// monitor domain's own check cadence still applies.
    /// </summary>
    public int? HealthCheckIntervalSeconds { get; set; }


    /// <summary>
    /// Gets or sets whether schema discovery is enabled for this connection.
    /// When false, ISchemaInformationService will return a failure without attempting discovery.
    /// </summary>
    public bool DiscoveryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the typed connection body for this header row.
    /// Populated on the read path by the provider after loading the typed body table row.
    /// Not persisted — the typed body is saved separately to its own table.
    /// </summary>
    /// <remarks>
    /// Why: [NotMapped] — this property is not a column on conn.Connection. The write path
    /// saves the typed body independently via its own provider. The read path populates this
    /// by dispatching on Implementation to the appropriate typed provider.
    /// </remarks>
    [NotMapped]
    public IConnectionImplementationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    /// <remarks>
    /// The non-generic view of <see cref="Configuration"/>. The platform service provider reads the
    /// implementation without naming this domain's implementation contract; netstandard2.0 rules out
    /// a default interface implementation, so each domain record states it.
    /// </remarks>
    IGenericConfiguration? IDomainConfiguration.ImplementationConfiguration => Configuration;

}
