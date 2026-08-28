using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Telemetry.Abstractions;

/// <summary>Non-generic marker so factories can be held without naming their type arguments.</summary>
public interface ITelemetryFactory
{
}

/// <summary>Builds a telemetry service from its implementation configuration.</summary>
/// <typeparam name="TService">The telemetry service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it builds from.</typeparam>
public interface ITelemetryFactory<TService, TConfiguration>
    : ITelemetryFactory, IServiceFactory<TService, TConfiguration>
    where TService : ITelemetryService
    where TConfiguration : IGenericConfiguration
{
}
