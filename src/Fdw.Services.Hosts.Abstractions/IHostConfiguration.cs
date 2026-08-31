using Fdw.Configuration;

namespace Fdw.Services.Hosts.Abstractions;

/// <summary>
/// The hosting domain configuration: names which implementation is configured and holds its settings.
/// </summary>
public interface IHostConfiguration
    : IPlatformServiceConfiguration<IHostImplementationConfiguration>
{
}
