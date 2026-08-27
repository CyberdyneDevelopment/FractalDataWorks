using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// Base configuration class for all secret manager types.
/// Generates the parent table <c>sec.SecretManager</c> which contains core identity fields shared by all secret manager types.
/// </summary>
/// <remarks>
/// <para>
/// This class serves two purposes:
/// <list type="bullet">
/// <item><description>As a header configuration for <c>IOptionsSnapshot&lt;List&lt;SecretManagerConfiguration&gt;&gt;</c> lookups</description></item>
/// <item><description>As the base class for type-specific configurations (EnvironmentVariableConfiguration, etc.)</description></item>
/// </list>
/// </para>
/// <para>
/// All type identity properties (ServiceType, ServiceOptionType, SectionName) are set via the constructor chain.
/// Derived classes call the protected constructor to set their specific values.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "SecretManager")]
public partial class SecretManagerConfiguration : IGenericConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerConfiguration"/> class.
    /// Default constructor for IOptions binding and header lookups.
    /// </summary>
    public SecretManagerConfiguration() : this("SecretManager", null, "SecretManagers")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretManagerConfiguration"/> class.
    /// Protected constructor for derived classes to set their type identity.
    /// </summary>
    /// <param name="serviceType">The service type (domain) - always "SecretManager".</param>
    /// <param name="serviceOptionType">The service option type (e.g., "EnvironmentVariable", "AzureKeyVault").</param>
    /// <param name="sectionName">The configuration section name for binding.</param>
    protected SecretManagerConfiguration(string serviceType, string? serviceOptionType, string sectionName)
    {
        ServiceType = serviceType;
        ServiceOptionType = serviceOptionType;
        SectionName = sectionName;
    }


    /// <summary>
    /// Gets or sets the durable logical identifier (matches sec.SecretManager.Id).
    /// </summary>
    // Why: NO Guid.NewGuid() default — DB owns identity assignment. A random default
    // here would silently propagate to child Get(domainConfigurationId) lookups when the mapper
    // failed to bind Id, returning ConfigurationNotFound for valid records.
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of this secret manager for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section name for configuration binding.
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the service type (domain) - always "SecretManager" for this configuration.
    /// </summary>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the service option type (e.g., "EnvironmentVariable", "AzureKeyVault").
    /// </summary>
    [ValuesFrom(typeof(SecretManagerTypes))]
    public string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the secret manager type name. Alias for <see cref="ServiceOptionType"/>.
    /// </summary>
    public string? SecretManagerType => ServiceOptionType;

    /// <summary>
    /// Gets or sets the optional description of this secret manager.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment this secret manager targets (e.g., Local, Dev, QA, Prod).
    /// </summary>
    [ValuesFrom(typeof(EnvironmentTypes))]
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the typed secret manager body for this header row.
    /// Populated on the read path after loading the typed body table row.
    /// Not persisted — the typed body is saved separately to its own table.
    /// </summary>
    // Why: [NotMapped] — not a column on sec.SecretManager. Written separately via typed provider.
    // Read path populates by dispatching on ServiceOptionType to the appropriate typed provider.
    [NotMapped]
    public ISecretManagerConfiguration? Configuration { get; set; }
}
