using Fdw.ServiceTypes;

namespace Fdw.Services.Telemetry.Abstractions;

/// <summary>
/// Resolves telemetry services by configuration name or id.
/// </summary>
public interface ITelemetryServiceProvider
    : IPlatformServiceProvider<ITelemetryService, ITelemetryImplementationConfiguration>
{
}
