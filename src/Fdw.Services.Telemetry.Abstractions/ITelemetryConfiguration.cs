using Fdw.Configuration;

namespace Fdw.Services.Telemetry.Abstractions;

/// <summary>
/// The telemetry domain configuration: names which implementation is configured and holds its settings.
/// </summary>
public interface ITelemetryConfiguration
    : IPlatformServiceConfiguration<ITelemetryImplementationConfiguration>
{
}
