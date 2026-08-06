using System;

namespace Fdw.Configuration;

/// <summary>
/// Wrapper for configuration instances that separates header metadata from domain-specific settings.
/// Enables heterogeneous collections of configurations with different settings types.
/// </summary>
/// <typeparam name="TSettings">The domain-specific configuration type.</typeparam>
/// <remarks>
/// <para>
/// This wrapper pattern allows providers to maintain dictionaries of configurations
/// where each entry may have different settings types (e.g., MsSqlConfiguration, OracleConfiguration).
/// </para>
/// <para>
/// The header properties (Id, Name, ServiceType) are common across all configurations,
/// while the Settings property contains the domain-specific configuration instance.
/// </para>
/// <example>
/// <code>
/// // Heterogeneous list of connection configurations
/// var configs = new List&lt;ConfigurationWrapper&lt;ConnectionConfiguration&gt;&gt;
/// {
///     new ConfigurationWrapper&lt;ConnectionConfiguration&gt;
///     {
///         Id = guid1,
///         Name = "Primary",
///         ServiceType = "MsSql",
///         Settings = new MsSqlConfiguration { Server = "localhost", Database = "mydb" }
///     },
///     new ConfigurationWrapper&lt;ConnectionConfiguration&gt;
///     {
///         Id = guid2,
///         Name = "Archive",
///         ServiceType = "Oracle",
///         Settings = new OracleConfiguration { Host = "remote", Port = 1521 }
///     }
/// };
///
/// // Provider builds lookup dictionaries
/// var byName = configs.ToDictionary(c => c.Name);
/// var byId = configs.ToDictionary(c => c.Id);
///
/// // Extract settings when needed
/// var config = byName["Primary"];
/// var settings = config.Settings;  // Returns MsSqlConfiguration
/// </code>
/// </example>
/// </remarks>
public sealed class ConfigurationWrapper<TSettings>
{
    /// <summary>
    /// Gets the unique identifier for this configuration instance.
    /// </summary>
    /// <remarks>
    /// This is the configuration instance ID, not the ServiceType ID.
    /// Multiple configurations can use the same ServiceType with different instance IDs.
    /// </remarks>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the name of this configuration for lookup and display.
    /// </summary>
    /// <remarks>
    /// Names must be unique within a domain (e.g., all connection names must be unique).
    /// Used for runtime lookup: <c>connectionProvider.GetConnection("Primary")</c>
    /// </remarks>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the service type discriminator for this configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This discriminator identifies which ServiceType implementation to use.
    /// Examples: "MsSql", "Oracle", "AzureEntra", "Auth0", "AzureKeyVault"
    /// </para>
    /// <para>
    /// Used for runtime dispatch via TypeCollections:
    /// <code>
    /// var serviceType = ConnectionTypes.ByName(config.ServiceType);
    /// var factory = _connectionServices[serviceType.Name];
    /// </code>
    /// </para>
    /// </remarks>
    public string ServiceType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when this configuration instance was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when this configuration was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; init; }

    /// <summary>
    /// Gets the domain-specific configuration settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This property contains the actual configuration instance with all domain-specific properties.
    /// The type varies based on ServiceType:
    /// </para>
    /// <list type="bullet">
    /// <item><description>ServiceType = "MsSql" → Settings is MsSqlConfiguration</description></item>
    /// <item><description>ServiceType = "Oracle" → Settings is OracleConfiguration</description></item>
    /// <item><description>ServiceType = "AzureEntra" → Settings is AzureEntraConfiguration</description></item>
    /// </list>
    /// <para>
    /// Providers extract settings at runtime:
    /// <code>
    /// var wrapper = _configsByName["Primary"];
    /// var settings = wrapper.Settings;  // MsSqlConfiguration instance
    /// var factory = _connectionServices[wrapper.ServiceType];
    /// await factory.Create(settings);
    /// </code>
    /// </para>
    /// </remarks>
    public TSettings Settings { get; init; } = default!;
}
