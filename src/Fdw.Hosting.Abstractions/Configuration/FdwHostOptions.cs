using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Root options for FDW host configuration.
/// Binds to the "FdwHost" section in appsettings.json.
/// </summary>
/// <remarks>
/// Configuration precedence (highest to lowest):
/// 1. Environment variables (FdwHost__Logging__MinimumLevel)
/// 2. Azure App Service deployment slot settings
/// 3. appsettings.{Environment}.json
/// 4. appsettings.json
/// 5. Default values in code
/// </remarks>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class FdwHostOptions
{
    /// <summary>
    /// The configuration section name for FDW host options.
    /// </summary>
    public const string SectionName = "FdwHost";

    /// <summary>
    /// Gets or sets the application name.
    /// Defaults to the entry assembly name.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Gets or sets the environment name (Development, Staging, Production).
    /// </summary>
    public string? Environment { get; set; }

    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Gets or sets the telemetry configuration.
    /// </summary>
    public TelemetryOptions Telemetry { get; set; } = new();

    /// <summary>
    /// Gets or sets the configuration database connection options.
    /// </summary>
    public ConfigurationConnectionOptions Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets feature flags for optional modules.
    /// </summary>
    public FeatureOptions Features { get; set; } = new();
}
