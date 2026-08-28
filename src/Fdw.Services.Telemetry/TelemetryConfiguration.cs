using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Services.Telemetry.Abstractions;

namespace Fdw.Services.Telemetry;

/// <summary>
/// The telemetry domain configuration: which telemetry implementation is configured, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Telemetry")]
public partial class TelemetryConfiguration : ITelemetryConfiguration
{
    // Why no generated default: the store assigns identity. A value minted here reaches Get(id) as a
    // real-looking id matching no row, and the miss reads as a data problem rather than an unsaved record.
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this configuration is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section this domain reads.</summary>
    public string SectionName => "Telemetry";

    /// <summary>Gets the service category this configuration belongs to.</summary>
    public string ServiceType => "Telemetry";

    /// <summary>Gets or sets the option name selecting which telemetry implementation is configured.</summary>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the configuration of the implementation named by <see cref="ServiceOptionType"/>.</summary>
    public ITelemetryImplementationConfiguration? Configuration { get; set; }
}
