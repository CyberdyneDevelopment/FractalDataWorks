using System;
using Fdw.Configuration;

namespace Fdw.Services.SecretManagers;

/// <summary>
/// Base configuration class for all secret manager types.
/// This non-generic base class generates the parent table <c>sec.SecretManager</c>
/// which contains only core identity fields shared by all secret manager types.
/// </summary>
/// <remarks>
/// <para>
/// This base class provides only the core identity properties:
/// <list type="bullet">
/// <item><description>Id, Name - Identity from IGenericConfiguration</description></item>
/// <item><description>SecretManagerType - Discriminator identifying the type of secret manager</description></item>
/// <item><description>Description - Optional human-readable description</description></item>
/// </list>
/// All other properties are defined on child configuration classes.
/// </para>
/// </remarks>
public abstract partial class SecretManagerConfigurationBase : IGenericConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this configuration instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Gets or sets the name of this configuration for lookup and display.
    /// This name must be unique within the secret manager type.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for this configuration in appsettings or database.
    /// Must be overridden by derived classes to specify binding path.
    /// </summary>
    public abstract string SectionName { get; }

    /// <summary>
    /// Gets the service type (domain) - always "SecretManager" for this configuration.
    /// </summary>
    public string ServiceType => "SecretManager";

    /// <summary>
    /// Gets the service option type (implementation variant) this configuration is for.
    /// Alias for <see cref="SecretManagerType"/>.
    /// </summary>
    public string? ServiceOptionType => SecretManagerType;

    /// <summary>
    /// Gets the secret manager type name this configuration is for.
    /// This discriminator is used by the SecretManagerProvider to determine which factory to use.
    /// </summary>
    public abstract string SecretManagerType { get; }

    /// <summary>
    /// Gets or sets an optional description for this secret manager configuration.
    /// </summary>
    public string? Description { get; set; }

}
