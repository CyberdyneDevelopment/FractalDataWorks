using System;
using Fdw.Configuration;

namespace Fdw.Configuration.Abstractions;

/// <summary>
/// Base class for all configuration types in the Fdw framework.
/// Provides common metadata properties for immutable configuration instances.
/// </summary>
/// <typeparam name="T">The derived configuration type (for type-safe chaining).</typeparam>
/// <remarks>
/// <para>
/// This base class provides:
/// <list type="bullet">
/// <item><description>Identity (Id, Name, SectionName)</description></item>
/// <item><description>Audit timestamps (CreatedAt, ModifiedAt)</description></item>
/// <item><description>Immutability via init-only properties</description></item>
/// </list>
/// </para>
/// <para>
/// Configuration discriminators are defined on domain-specific interfaces
/// (e.g., ConnectionConfiguration.ConnectionType).
/// </para>
/// <example>
/// <code>
/// public class EmailSettings : ConfigurationBase&lt;EmailSettings&gt;
/// {
///     public override string SectionName => "Email";
///
///     public string SmtpHost { get; init; } = "smtp.gmail.com";
///     public int SmtpPort { get; init; } = 587;
/// }
/// </code>
/// </example>
/// </remarks>
public abstract class ConfigurationBase<T> : IGenericConfiguration<T>
    where T : ConfigurationBase<T>
{
    /// <summary>
    /// Gets the unique identifier for this configuration instance.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the name of this configuration for lookup and display.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the section name for this configuration in appsettings or database.
    /// Must be overridden by derived classes.
    /// </summary>
    /// <example>"Email", "Database", "Notification"</example>
    public abstract string SectionName { get; }

    /// <summary>
    /// Gets the service type (domain) for this configuration.
    /// Must be overridden by derived classes.
    /// </summary>
    /// <example>"Connection", "Authentication", "Notification"</example>
    public abstract string ServiceType { get; }

    /// <summary>
    /// Gets the service option type (implementation variant) for this configuration.
    /// </summary>
    /// <example>"MsSql", "Jwt", "Email"</example>
    public virtual string? ServiceOptionType { get; set; }

    /// <summary>
    /// Gets the timestamp when this configuration instance was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when this configuration was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

}
