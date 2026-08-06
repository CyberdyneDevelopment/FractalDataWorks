namespace Fdw.Configuration;

using System;

/// <summary>
/// Base interface for all configuration objects in the Fdw framework.
/// Provides common properties for all configuration types.
/// </summary>
public interface IGenericConfiguration
{
    /// <summary>
    /// Gets the unique identifier for this configuration instance.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Gets the name of this configuration for lookup and display.
    /// Defaults to <see cref="string.Empty"/>; FluentValidation enforces non-empty at validation time.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets the section name for this configuration in appsettings.
    /// Defaults to <see cref="string.Empty"/>; FluentValidation enforces non-empty at validation time.
    /// </summary>
    string SectionName { get; }

    /// <summary>
    /// Gets the service type (domain) this configuration is for.
    /// Examples: "Connection", "Authentication", "Notification", "SecretManager", etc.
    /// Defaults to <see cref="string.Empty"/>; FluentValidation enforces non-empty at validation time.
    /// </summary>
    string ServiceType { get; }

    /// <summary>
    /// Gets the service option type (specific implementation) this configuration is for.
    /// Examples: "MsSql", "Jwt", "Email", "AzureKeyVault", etc.
    /// Used by providers to determine which factory to use.
    /// </summary>
    string? ServiceOptionType { get; }

}

/// <summary>
/// Generic configuration interface for type-safe configuration.
/// </summary>
/// <typeparam name="T">The concrete configuration type.</typeparam>
public interface IGenericConfiguration<T> : IGenericConfiguration
    where T : IGenericConfiguration<T>
{
}
