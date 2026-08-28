using Fdw.Configuration;

namespace Fdw.Services.Logging.Abstractions;

/// <summary>
/// The logging domain configuration: names which implementation is configured and holds its settings.
/// </summary>
public interface ILoggingConfiguration
    : IPlatformServiceConfiguration<ILoggingImplementationConfiguration>
{
}
