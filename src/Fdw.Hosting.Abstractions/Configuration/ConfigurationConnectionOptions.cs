using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Configuration database connection options.
/// Binds to the "FdwHost:Configuration" section in appsettings.json.
/// </summary>
/// <remarks>
/// The configuration connection is used for storing and retrieving FDW configuration
/// such as DataSet definitions, connection metadata, and other runtime configuration.
/// </remarks>
[ExcludeFromCodeCoverage]
public class ConfigurationConnectionOptions
{
    /// <summary>
    /// Gets or sets whether to use a configuration database. Default is false.
    /// When false, configuration is loaded from TypeCollections and appsettings only.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the connection name in the Connections section to use for configuration.
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: FdwHost__Configuration__ConnectionName
    /// </remarks>
    public string? ConnectionName { get; set; } = "PlatformConfiguration";

    /// <summary>
    /// Gets or sets whether to automatically initialize the configuration schema on startup.
    /// Default is true when configuration is enabled.
    /// </summary>
    public bool AutoBootstrap { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to import TypeCollection-defined configurations on startup.
    /// Default is true.
    /// </summary>
    public bool ImportTypeCollections { get; set; } = true;
}
