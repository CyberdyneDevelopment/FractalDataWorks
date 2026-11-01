using Fdw.Configuration;

namespace Fdw.Services.Telemetry.Abstractions;

/// <summary>
/// The contract every telemetry implementation's configuration satisfies.
/// </summary>
/// <remarks>
/// In reference-api today these are four reads against <c>IConfiguration</c> with inline defaults —
/// <c>Configuration["OpenTelemetry:ServiceName"] ?? "ReferenceApi"</c> and three
/// <c>GetValue(key, true/false)</c> calls — so the service name silently falls back and the toggles
/// have defaults no configuration records.
/// </remarks>
public interface ITelemetryImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the service name reported on every trace and metric.</summary>
    string ServiceName { get; set; }

    /// <summary>Gets or sets a value indicating whether tracing is collected.</summary>
    bool TracingEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether metrics are collected.</summary>
    bool MetricsEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether traces are also written to the console.</summary>
    bool ExportToConsole { get; set; }

    /// <summary>Gets or sets the OTLP collector endpoint, when one is used.</summary>
    string? CollectorEndpoint { get; set; }
}
