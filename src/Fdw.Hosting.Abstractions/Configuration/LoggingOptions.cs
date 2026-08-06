using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Hosting.Abstractions.Configuration;

/// <summary>
/// Logging configuration options for Serilog integration.
/// Binds to the "FdwHost:Logging" section in appsettings.json.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public class LoggingOptions
{
    /// <summary>
    /// Gets or sets the minimum log level. Default is "Information".
    /// Valid values: Verbose, Debug, Information, Warning, Error, Fatal
    /// </summary>
    /// <remarks>
    /// Can be overridden via environment variable: FdwHost__Logging__MinimumLevel
    /// </remarks>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets log level overrides for specific namespaces.
    /// </summary>
    /// <example>
    /// "LevelOverrides": {
    ///   "Microsoft.AspNetCore": "Warning",
    ///   "Microsoft.EntityFrameworkCore": "Warning",
    ///   "System": "Warning"
    /// }
    /// </example>
    public IDictionary<string, string> LevelOverrides { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Microsoft"] = "Warning",
        ["Microsoft.AspNetCore"] = "Warning",
        ["System"] = "Warning"
    };

    /// <summary>
    /// Gets or sets whether to enrich logs with machine name. Default is true.
    /// </summary>
    public bool EnrichWithMachineName { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with environment name. Default is true.
    /// </summary>
    public bool EnrichWithEnvironment { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with thread ID. Default is true.
    /// </summary>
    public bool EnrichWithThreadId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with correlation ID. Default is true.
    /// </summary>
    public bool EnrichWithCorrelationId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enrich logs with tenant ID when multitenancy is enabled. Default is true.
    /// </summary>
    public bool EnrichWithTenantId { get; set; } = true;

    /// <summary>
    /// Gets or sets console sink options.
    /// </summary>
    public ConsoleSinkOptions Console { get; set; } = new();

    /// <summary>
    /// Gets or sets file sink options. Null disables file logging.
    /// </summary>
    public FileSinkOptions? File { get; set; }

    /// <summary>
    /// Gets or sets Seq sink options. Null disables Seq logging.
    /// </summary>
    public SeqSinkOptions? Seq { get; set; }

    /// <summary>
    /// Gets or sets OpenTelemetry sink options. Null disables OTLP log export.
    /// </summary>
    public OpenTelemetrySinkOptions? OpenTelemetry { get; set; }
}
