using Fdw.Services.Abstractions;

namespace Fdw.Services.Telemetry.Abstractions;

/// <summary>
/// A host: one configured HTTP request pipeline, resolved by name like any other service.
/// </summary>
public interface ITelemetryService : IServiceOption
{
}
