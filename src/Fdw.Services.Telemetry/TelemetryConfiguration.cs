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
public partial class TelemetryConfiguration : DomainConfigurationBase, ITelemetryConfiguration
{






}
