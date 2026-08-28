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
/// All type identity properties (ServiceType, ServiceOptionType, SectionName) are set via the constructor chain.
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
    public ConnectionConfiguration() : this("Connection", null, "Connections")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "Connection".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "MsSql", "Rest", "Http").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected ConnectionConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }


    /// <summary>
    /// Gets or sets the durable logical identifier (matches conn.Connection.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this connection for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "Connection" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service option type (e.g., "MsSql", "Rest", "Http").
    /// </summary>
    [ValuesFrom(typeof(ConnectionTypes))]
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the connection type name. Alias for <see cref="ServiceOptionType"/>.
    /// </summary>
    public string? ConnectionType => ServiceOptionType;

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
    /// by dispatching on ServiceOptionType to the appropriate typed provider.
    /// </remarks>
    [NotMapped]
    public IConnectionImplementationConfiguration? Configuration { get; set; }
}
