using Fdw.Services.Abstractions;

namespace Fdw.Services.Logging.Abstractions;

/// <summary>
/// A logging service: one configured logging pipeline, resolved by name like any other service.
/// </summary>
public interface ILoggingService : IServiceOption
{
}
