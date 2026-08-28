using System;
using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Telemetry.Abstractions;

/// <summary>Marker for the telemetry option set — the return type of ById, ByName and All.</summary>
public interface ITelemetryType : IServiceType
{
}

/// <summary>The closed form every telemetry option satisfies.</summary>
/// <typeparam name="TService">The telemetry service the option's factory creates.</typeparam>
/// <typeparam name="TConfiguration">The implementation configuration it binds to.</typeparam>
/// <typeparam name="TFactory">The factory the option registers.</typeparam>
public interface ITelemetryType<TService, TConfiguration, TFactory>
    : IServiceType<Guid, TService, TFactory, TConfiguration>, ITelemetryType
    where TService : ITelemetryService
    where TConfiguration : IGenericConfiguration
    where TFactory : ITelemetryFactory<TService, TConfiguration>
{
}
