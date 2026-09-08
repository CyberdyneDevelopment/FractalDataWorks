using Fdw.ServiceTypes;

namespace Fdw.Services.Logging.Abstractions;

/// <summary>
/// Resolves logging services by configuration name or id.
/// </summary>
public interface ILoggingServiceProvider
    : IPlatformServiceProvider<ILoggingService, ILoggingImplementationConfiguration>
{
}
